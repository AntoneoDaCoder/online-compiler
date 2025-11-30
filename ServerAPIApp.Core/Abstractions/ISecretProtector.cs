using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.Abstractions
{
    public interface ISecretProtector
    {
        string Protect(string plain);
        string Unprotect(string protectedText);
    }
}
