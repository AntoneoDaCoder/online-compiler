using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record LogoutUserCase(Guid InitiatorId, Guid TokenUserId) : IRequest;
}
