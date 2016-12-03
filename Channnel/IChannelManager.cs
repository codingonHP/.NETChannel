namespace Channnel
{
    public interface IChannelManager
    {
        void AddNewClient(Client client);
        void RemoveClient(Client client);
        bool ClientExists(Client client);
        InvocationScope GetClientInvocationScope(string clientId, InvocationScope invocationScope);
        Client GetClient(string clientId);
    }
}