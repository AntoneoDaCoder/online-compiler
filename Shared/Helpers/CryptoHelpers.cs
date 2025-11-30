using System.Security.Cryptography;
using System.Text;

namespace Shared.Helpers
{
    public static class CryptoHelpers
    {
        public static string ComputeSha256Hex(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim()));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static string GenerateSecureRandomString(int len = 32)
        {
            var b = new byte[len];
            RandomNumberGenerator.Fill(b);
            return Convert.ToBase64String(b);
        }
    }
}
