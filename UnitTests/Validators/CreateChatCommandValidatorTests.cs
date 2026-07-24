using FluentValidation.TestHelper;
using LightChat.Core.Features.Chats.CreateChat;

namespace LightChat.Core.Tests.Validators
{
    public class CreateChatCommandValidatorTests
    {
        private readonly CreateChatValidator _validator = new();

        #region Have_Error
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ab")]
        public void Should_Have_Error_When_Name_Is_Invalid(string invalidName)
        {
            var command = new CreateChatCommand(invalidName, Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.Name);
        }

        [Fact]
        public void Should_Have_Error_When_Creator_Guid_Is_Invalid()
        {
            var command = new CreateChatCommand("validName", Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(c => c.CreatorUserId);
        }
        #endregion

        #region Not_Have_Error
        [Theory]
        [InlineData("validName")]
        [InlineData("goketsu")]
        public void Should_Not_Have_Error_When_Name_Is_Valid(string validName)
        {
            var command = new CreateChatCommand(validName, Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Name);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Creator_Guid_Is_Valid()
        {
            var command = new CreateChatCommand("validName", Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.CreatorUserId);
        }
        #endregion
    }
}