using FluentValidation.TestHelper;
using LightChat.Core.Features.Users.UserJwtAuthorize;

namespace LightChat.Core.Tests.Validators
{
    public class UserJwtAuthorizeCommandValidatorTests
    {
        private readonly UserJwtAuthorizeValidator _validator = new();

        #region Have_Error
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        public void Should_Have_Error_When_Username_Is_Invalid(string invalidUsername)
        {
            var query = new UserJwtAuthorizeQuery(invalidUsername, "Password123!");

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(c => c.Username);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12345")]
        public void Should_Have_Error_When_Password_Is_Invalid(string invalidPassword)
        {
            var query = new UserJwtAuthorizeQuery("ValidUser", invalidPassword);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(c => c.Password);
        }
        #endregion

        #region Not_Have_Error
        [Theory]
        [InlineData("validName")]
        [InlineData("goketsu")]
        public void Should_Not_Have_Error_When_Username_Is_Valid(string validUsername)
        {
            var query = new UserJwtAuthorizeQuery(validUsername, "Password123!");

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(c => c.Username);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Password_Is_Valid()
        {
            var query = new UserJwtAuthorizeQuery("ValidUser", "Password123!");

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(c => c.Password);
        }
        #endregion
    }
}