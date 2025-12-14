using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.Configs
{
    public class SecretProtectionOptions
    {
        public string Algorithm { get; set; } = "AesGcm";
        public string FixedKeyBase64 { get; set; } = "";
    }
}
