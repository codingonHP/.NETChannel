using System;
using System.Collections.Generic;

namespace Channnel
{
    public class ChannelManager : IChannelManager
    {
        static readonly object LockSwitch = new object();
        readonly Dictionary<string, Client> _clientList = new Dictionary<string, Client>();

        public void AddNewClient(Client client)
        {
            lock (LockSwitch)
            {
                var isConfigValid = ValidateClientConfig(client);
                if (!ClientExists(client) && isConfigValid)
                {
                    _clientList.Add(client.ThreadId, client);
                }

                if (!isConfigValid)
                {
                    throw new InvalidOperationException($"cannot add {nameof(client)}. Please check your client config");
                }
            }
        }

        public void RemoveClient(Client client)
        {
            lock (LockSwitch)
            {
                if (ClientExists(client))
                {
                    _clientList.Remove(client.ThreadId);
                }
            }
        }

        public bool ClientExists(Client client)
        {
            lock (LockSwitch)
            {
                return _clientList.ContainsKey(client.ThreadId);
            }
        }

        public ClientConfig GetClientConfig(string clientId)
        {
            lock (LockSwitch)
            {
                if (ClientExists(new Client { ThreadId = clientId }))
                {
                    var client = _clientList[clientId];
                    return client.ClientConfig;
                }

                return null;
            }
        }

        public bool ValidateClientConfig(Client client)
        {
            if (client.ClientConfig != null)
            {
                var c = client.ClientConfig;
                c.ValidateSettings();
            }

            return true;
        }
    }
}
