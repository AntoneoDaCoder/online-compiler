using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record RefreshAccessTokenCase(Guid InitiatorId, string AccessToken) : IRequest<string>;
}
