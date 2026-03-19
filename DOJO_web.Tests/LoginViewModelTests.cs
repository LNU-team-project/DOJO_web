using System.ComponentModel.DataAnnotations;
using DOJO2.Presentation.ViewModels;
using Xunit;

public class LoginViewModelTests
{
    private static IList<ValidationResult> ValidateModel(LoginViewModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void LoginViewModel_WhenValid_PassesValidation()
    {
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = "secret",
            RememberMe = true
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null, "Введіть адресу пошти.")]
    [InlineData("", "Введіть адресу пошти.")]
    [InlineData("not-an-email", "Введіть коректну адресу пошти.")]
    public void LoginViewModel_WhenEmailInvalid_FailsValidation(string? email, string expectedMessage)
    {
        var model = new LoginViewModel
        {
            Email = email ?? string.Empty,
            Password = "secret",
            RememberMe = false
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.ErrorMessage == expectedMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void LoginViewModel_WhenPasswordMissing_FailsValidation(string? password)
    {
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = password ?? string.Empty,
            RememberMe = false
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.ErrorMessage == "Введіть пароль.");
    }
}