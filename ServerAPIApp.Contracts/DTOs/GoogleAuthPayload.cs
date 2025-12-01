using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public record GoogleAuthPayload(string Subject, string Email, string Name);
}
