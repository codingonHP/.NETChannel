namespace Channnel
{
    public class Client
    {
        public string Name { get; set; }
        public string ThreadId { get; set; }
        public ClientConfig ClientConfig { get; set; }
        public InvocationScope InvocationScope { get; set; }
    }
}
