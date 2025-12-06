using Google.Apis.Auth;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.Abstractions;

namespace ServerAPIApp.Core.Services
{
    public class GoogleAuthTokenValidator : IGoogleAuthTokenValidator
    {
        private IReadOnlyCollection<string> _allowedAudiences;
        private GoogleJsonWebSignature.ValidationSettings? _validationSettings;

        public GoogleAuthTokenValidator(IEnumerable<string> allowedClientIds)
        {
            _allowedAudiences = (allowedClientIds ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();

            _validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _allowedAudiences.Count > 0 ? _allowedAudiences.ToList() : null
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
