using FluentValidation.TestHelper;
using LightChat.Core.Features.Users.UserRegister;

namespace LightChat.Core.Tests.Validators
{
    public class UserRegisterCommandValidatorTests
    {
        private readonly UserRegisterValidator _validator = new();
        #region Have_Error
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        public void Should_Have_Error_When_Username_Is_Invalid(string invalidUsername)
        {
            var command = new UserRegisterCommand(invalidUsername, "test@mail.com", "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.Username);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12345")]
        public void Should_Have_Error_When_Password_Is_Invalid(string invalidPassword)
        {
            var command = new UserRegisterCommand("ValidUser", "test@mail.com", invalidPassword);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.Password);
        }

        [Theory]
        [InlineData("plainaddress")]
        [InlineData("#@%^%#$@#$@#.com")]
        [InlineData("@example.com")]
        [InlineData("email.example.com")]
        [InlineData("email@example@example.com")]
        public void Should_Have_Error_When_Email_Is_Invalid(string invalidEmail)
        {
            var command = new UserRegisterCommand("ValidUser", invalidEmail, "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.Email);
        }
        #endregion

        #region Not_Have_Error
        [Fact]
        public void Should_Not_Have_Error_When_Username_Is_Valid()
        {
            var command = new UserRegisterCommand("validUsername", "test@mail.com", "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Username);
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name+tag+sorting@example.com")]
        [InlineData("user.name@example.co.uk")]
        public void Should_Not_Have_Error_When_Email_Is_Valid(string validEmail)
        {
            var command = new UserRegisterCommand("ValidUser", validEmail, "Password123!");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Email);
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("asgg;ljv")]
        public void Should_Not_Have_Error_When_Password_Is_Valid(string validPassword)
        {
            var command = new UserRegisterCommand("ValidUser", "test@mail.com", validPassword);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Password);
        }
        #endregion
    }
}