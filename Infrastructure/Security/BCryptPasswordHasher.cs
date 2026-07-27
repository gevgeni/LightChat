using LightChat.Core.Interfaces;

namespace LightChat.Infrastructure.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash))
                return true;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }
    }
}