using MediatR;
using FluentValidation;

namespace LightChat.Core.Features.Users
{
    public record UserJwtAuthorizeQuery(string Username, string Password) : IRequest<JwtTokenDto>;
    public record JwtTokenDto(string TokenString);
}