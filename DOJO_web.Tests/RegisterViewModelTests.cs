using System.ComponentModel.DataAnnotations;
using DOJO2.Presentation.ViewModels;
using Xunit;

public class RegisterViewModelTests
{
    private static IList<ValidationResult> ValidateModel(RegisterViewModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void RegisterViewModel_WhenValid_PassesValidation()
    {
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "user@example.com",
            Password = "secret1",
            ConfirmPassword = "secret1"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("", "Введіть адресу пошти.")]
    [InlineData("not-an-email", "Введіть коректну адресу пошти.")]
    public void RegisterViewModel_WhenEmailInvalid_FailsValidation(string email, string expectedMessage)
    {
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = email,
            Password = "secret1",
            ConfirmPassword = "secret1"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void RegisterViewModel_WhenPasswordTooShort_FailsValidation()
    {
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "user@example.com",
            Password = "12345",
            ConfirmPassword = "12345"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("щонайменше 6"));
    }

    [Fact]
    public void RegisterViewModel_WhenPasswordsDoNotMatch_FailsValidation()
    {
        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "user@example.com",
            Password = "secret1",
            ConfirmPassword = "secret2"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("не співпадають"));
    }
}
