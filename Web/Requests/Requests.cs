namespace LightChat.Web.Requests
{
    public record CreateUserRequest(string Username, string Email, string Password);
    public record CreateChatRequest(string Name);
    public record AddMemberRequest(Guid UserId);
    public record LoginRequest(string Username, string Password);
    public record CreateDirectChatRequest(Guid TargetUserId);
}