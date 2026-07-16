using MediatR;
using FluentValidation;

namespace LightChat.Core.Features.Users
{
    public record UserRegisterCommand(string Username, string Email, string Password) : IRequest<UserDto>;
}