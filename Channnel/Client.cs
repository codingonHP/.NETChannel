using System.Collections.Generic;

namespace Channnel
{
    public class Client
    {
        public string Name { get; set; }
        public string ThreadId { get; set; }
        public InvocationScope InvocationScope { get; set; }
        public List<InvocationScope> InvocationScopes = new List<InvocationScope>();
    }
}
