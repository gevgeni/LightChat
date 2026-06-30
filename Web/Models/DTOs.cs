namespace LightChat.Web.Models
{
    public record CreateUserDto(string Username, string Email);
    public record CreateChatDto(string Name);
    public record AddMemberDto(Guid ChatId, Guid UserId);
    public record LoginRequest(string Username);
}