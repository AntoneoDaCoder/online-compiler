using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public record LoginDataDto(Guid UserId, string Name, IEnumerable<string> Roles, DateTimeOffset AccountCreatedAt, string AccessToken)
    {
        public static LoginDataDto From(Guid userId, string name, IEnumerable<string> roles, DateTimeOffset accountCreatedAt, string accessToken)
        {
            return new LoginDataDto(userId, name, roles, accountCreatedAt, accessToken);
        }
    }
}
