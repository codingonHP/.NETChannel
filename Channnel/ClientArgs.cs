using System;
using System.Collections.Generic;

namespace Channnel
{
    public class ClientArgs : EventArgs
    {
        public string ClientId { get; set; }
        public InvocationScope InvocationScope { get; set; }
        public List<InvocationScope> InvocationScopes { get; set; }
    }
}