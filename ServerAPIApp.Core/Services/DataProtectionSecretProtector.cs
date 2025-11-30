using ServerAPIApp.Core.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace ServerAPIApp.Core.Services
{
    public class DataProtectionSecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;
        public DataProtectionSecretProtector(IDataProtectionProvider dp)
        {
            _protector = dp.CreateProtector("ServerAPIApp.UserEntity.Protect");
        }
        public string Protect(string plain) => _protector.Protect(plain);
        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }
}
