using System.Collections.Generic;
using Channel;

namespace Channnel
{
    public class Client
    {
        public string ClientName { get; private set; }
        public Signal Signal = new Signal();
        public List<InvocationScope> InvocationScopes = new List<InvocationScope>();

        public Client()
        {
            ClientName = Helpers.GetInvocationScopeMethodName(4);
        }
    }
}
