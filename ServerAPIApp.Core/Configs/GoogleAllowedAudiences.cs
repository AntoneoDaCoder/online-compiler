using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.Configs
{
    public class GoogleAllowedAudiences
    {
        public IEnumerable<string> ClientIds { get; set; } = [];
    }
}
