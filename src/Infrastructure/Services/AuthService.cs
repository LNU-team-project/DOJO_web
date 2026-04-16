using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Results;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Encodings.Web;

namespace DOJO2.Infrastructure.Services;

public interface IAuthService
{
    Task<Result<bool>> LoginAsync(string email, string password, bool rememberMe);
    Task<Result<bool>> RegisterAsync(string userName, string email, string password);
    Task<Result<bool>> LogoutAsync();
    Task<Result<bool>> ForgotPasswordAsync(string email, string callbackUrl);
    Task<Result<bool>> ResetPasswordAsync(string email, string code, string newPassword);
    Task<Result<AppUser>> GetUserAsync(string userId);
    Task<Result<bool>> SendEmailConfirmationAsync(string email, string callbackUrl);
    Task<Result<bool>> ConfirmEmailAsync(int userId, string code);
    Task<Result<bool>> SendTestEmailAsync(string email);
}

public class AuthService : IAuthService
{
    private const string EmptyEmailMessage = "Email не може бути порожним";
    private const string UserNotFoundMessage = "Користувача не знайдено";
    private const string BlockedUserMessage = "Ваш обліковий запис заблоковано. Зверніться до адміністратора.";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender,
        ILogger<AuthService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<bool>> LoginAsync(string email, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба входу з порожним email");
            return Result<bool>.FailureResult(EmptyEmailMessage);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Спроба входу з порожним паролем");
            return Result<bool>.FailureResult("Пароль не може бути порожним");
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            _logger.LogWarning("Спроба входу з невідомим email: {Email}", email);
            return Result<bool>.FailureResult("Невірна пошта або пароль.");
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user.UserName ?? string.Empty,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("Спроба входу заблокованого користувача: {UserName}", user.UserName);
            return Result<bool>.FailureResult(BlockedUserMessage);
        }

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Невдалий вхід для користувача: {UserName}", user.UserName);
            return Result<bool>.FailureResult("Невірна пошта або пароль.");
        }

        _logger.LogInformation("Користувач успішно увійшов: {UserName}", user.UserName);
        return Result<bool>.SuccessResult(true, "Успішний вхід");
    }

    public async Task<Result<bool>> RegisterAsync(string userName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("Спроба реєстрації з порожним ім'ям");
            return Result<bool>.FailureResult("Ім'я користувача не може бути порожним");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба реєстрації з порожним email");
            return Result<bool>.FailureResult(EmptyEmailMessage);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Спроба реєстрації з порожним паролем");
            return Result<bool>.FailureResult("Пароль не може бути порожним");
        }

        var user = new AppUser
        {
            UserName = userName.Trim(),
            Email = email.Trim()
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при створенні користувача: {Errors}", string.Join(", ", errors));
            return Result<bool>.FailureResult("Не вдалося зареєструватися", errors);
        }

        _logger.LogInformation("Користувач успішно створений: {UserName}", user.UserName);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return Result<bool>.SuccessResult(true, "Успішна реєстрація");
    }

    public async Task<Result<bool>> LogoutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Користувач вийшов з системи");
            return Result<bool>.SuccessResult(true, "Успішний вихід");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при виході користувача");
            return Result<bool>.FailureResult("Не вдалося вийти з системи");
        }
    }

    public async Task<Result<bool>> ForgotPasswordAsync(string email, string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба скидання пароля з порожним email");
            return Result<bool>.FailureResult(EmptyEmailMessage);
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            _logger.LogWarning("Користувач з email {Email} не знайдено для скидання пароля", email);
            // Не розкриваємо що користувача немає
            return Result<bool>.SuccessResult(true, "Якщо такий email існує, на нього будуть відправлені інструкції");
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = callbackUrl.Replace("PLACEHOLDER", Uri.EscapeDataString(code), StringComparison.Ordinal);

        try
        {
            await _emailSender.SendEmailAsync(
                email.Trim(),
                "Скидання пароля",
            $"Будь ласка, скиньте пароль натиснувши на посилання: <a href='{HtmlEncoder.Default.Encode(resetUrl)}'>посилання</a>");

            _logger.LogInformation("Email для скидання пароля відправлено на {Email}", email);
            return Result<bool>.SuccessResult(true, "Інструкції скидання пароля відправлено на вашу поштову скриньку");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправленні email для скидання пароля");
            return Result<bool>.FailureResult("Не вдалося відправити email для скидання пароля");
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(string email, string code, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба скидання пароля з порожним email");
            return Result<bool>.FailureResult("Email не може бути порожним");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Спроба скидання пароля з порожним кодом");
            return Result<bool>.FailureResult("Код скидання пароля невалідний");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _logger.LogWarning("Спроба скидання пароля з порожним новим паролем");
            return Result<bool>.FailureResult("Новий пароль не може бути порожним");
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            _logger.LogWarning("Користувач з email {Email} не знайдено для скидання пароля", email);
            return Result<bool>.FailureResult(UserNotFoundMessage);
        }

        var result = await _userManager.ResetPasswordAsync(user, code, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при скиданні пароля: {Errors}", string.Join(", ", errors));
            return Result<bool>.FailureResult("Не вдалося скинути пароль", errors);
        }

        _logger.LogInformation("Пароль успішно скинут для користувача {Email}", email);
        return Result<bool>.SuccessResult(true, "Пароль успішно скинут");
    }

    public async Task<Result<AppUser>> GetUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Спроба отримати користувача з порожним userId");
            return Result<AppUser>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено", userId);
            return Result<AppUser>.FailureResult(UserNotFoundMessage);
        }

        return Result<AppUser>.SuccessResult(user, "Користувача отримано");
    }

    public async Task<Result<bool>> SendEmailConfirmationAsync(string email, string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба відправити підтвердження email з порожним email");
            return Result<bool>.FailureResult(EmptyEmailMessage);
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            _logger.LogWarning("Користувач з email {Email} не знайдено для підтвердження email", email);
            return Result<bool>.FailureResult(UserNotFoundMessage);
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = callbackUrl.Replace("PLACEHOLDER", Uri.EscapeDataString(code), StringComparison.Ordinal);

        try
        {
            await _emailSender.SendEmailAsync(
                email.Trim(),
                "Підтвердження email",
                $"Будь ласка, підтвердіть свій email натиснувши на посилання: <a href='{HtmlEncoder.Default.Encode(confirmUrl)}'>підтвердити</a>");

            _logger.LogInformation("Email підтвердження відправлено на {Email}", email);
            return Result<bool>.SuccessResult(true, "Лист з підтвердженням надіслано");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправленні email підтвердження");
            return Result<bool>.FailureResult("Не вдалося відправити email підтвердження");
        }
    }

    public async Task<Result<bool>> ConfirmEmailAsync(int userId, string code)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба підтвердити email з невалідним userId: {UserId}", userId);
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Спроба підтвердити email з порожним кодом для користувача {UserId}", userId);
            return Result<bool>.FailureResult("Код підтвердження невалідний");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено для підтвердження email", userId);
            return Result<bool>.FailureResult(UserNotFoundMessage);
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при підтвердженні email: {Errors}", string.Join(", ", errors));
            return Result<bool>.FailureResult("Не вдалося підтвердити email", errors);
        }

        _logger.LogInformation("Email підтверджено для користувача {UserId}", userId);
        return Result<bool>.SuccessResult(true, "Email успішно підтверджено");
    }

    public async Task<Result<bool>> SendTestEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Спроба відправити тестовий email з порожним email");
            return Result<bool>.FailureResult(EmptyEmailMessage);
        }

        try
        {
            await _emailSender.SendEmailAsync(
                email.Trim(),
                "SendGrid Test",
                "Це тестовий email з SendGrid.");

            _logger.LogInformation("Тестовий email відправлено на {Email}", email);
            return Result<bool>.SuccessResult(true, $"Тестовий email відправлено на {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправленні тестового email");
            return Result<bool>.FailureResult("Не вдалося відправити тестовий email");
        }
    }
}

