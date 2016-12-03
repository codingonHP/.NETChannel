namespace Channnel
{
    public interface IChannelManager
    {
        void AddNewClient(Client client);
        void RemoveClient(Client client);
        bool ClientExists(Client client);
        ClientConfig GetClientConfig(string clientId);
    }
}