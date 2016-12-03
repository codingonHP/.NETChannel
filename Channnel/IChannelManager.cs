namespace Channnel
{
    public interface IChannelManager
    {
        void AddNewClient(InvocationScope invocationScope);
        void RemoveClient(string clientId);
        bool ClientExists(string clientId);
        InvocationScope GetClientInvocationScope(string clientId, string invocationScope);
        Client GetClient(string clientId);
    }
}