using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;

namespace ServerAPIApp.Core.Services
{
    public class GoogleAuthTokenValidator : IGoogleAuthTokenValidator
    {
        private IEnumerable<string> _allowedAudiences;
        private GoogleJsonWebSignature.ValidationSettings? _validationSettings;

        public GoogleAuthTokenValidator(IOptions<GoogleAllowedAudiences> allowedAudiences)
        {
            _allowedAudiences = allowedAudiences.Value.ClientIds;

            _validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _allowedAudiences.Any() ? _allowedAudiences : []
            };
        }

        public async Task<GoogleAuthPayload?> ValidateTokenAsync
            (string idToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idToken))
                    return null;

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, _validationSettings);

                if (payload == null) return null;

                if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Name))
                    return null;

                return new GoogleAuthPayload(
                    Subject: payload.Subject,
                    Email: payload.Email,
                    Name: payload.Name
                );
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }
    }
}
