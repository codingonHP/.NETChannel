using System;
using System.Collections.Generic;
using System.Linq;

namespace Channnel
{
    public class ChannelManager : IChannelManager
    {
        static readonly object LockSwitch = new object();
        readonly Dictionary<string, Client> _clientList = new Dictionary<string, Client>();

        public event Action<object, ClientArgs> ClientAdded;
        public event Action<object, ClientArgs> ClientRemoved;

        public void AddNewInvocationScope(InvocationScope invocationScope)
        {
            lock (LockSwitch)
            {
                if (!ClientExists(invocationScope.ThreadId))
                {
                    var client = new Client();
                    client.InvocationScopes.Add(invocationScope);
                    _clientList.Add(invocationScope.ThreadId, client);

                    OnClientAdded(this, new ClientArgs
                    {
                        ClientId = invocationScope.ThreadId,
                        InvocationScope = invocationScope,
                        InvocationScopes = client.InvocationScopes
                    });

                    return;
                }

                //client doesn't exist

                var savedClient = GetClient(invocationScope.ThreadId);
                savedClient.InvocationScopes.Add(invocationScope);

                OnClientAdded(this, new ClientArgs
                {
                    ClientId = invocationScope.ThreadId,
                    InvocationScope = invocationScope,
                    InvocationScopes = savedClient.InvocationScopes
                });
            }
        }

        public void RemoveClient(string clientId)
        {
            lock (LockSwitch)
            {
                if (ClientExists(clientId))
                {
                    _clientList.Remove(clientId);
                }
            }
        }

        public bool ClientExists(string clientId)
        {
            lock (LockSwitch)
            {
                return _clientList.ContainsKey(clientId);
            }
        }

        public InvocationScope GetClientInvocationScope(string clientId, string invocationScope)
        {
            lock (LockSwitch)
            {
                if (ClientExists(clientId))
                {
                    var client = _clientList[clientId];
                    return client.InvocationScopes.Single(s => s.InvocationScopeName == invocationScope);
                }

                return null;
            }
        }

        public Client GetClient(string clientId)
        {
            lock (LockSwitch)
            {
                if (ClientExists(clientId))
                {
                    var client = _clientList[clientId];
                    return client;
                }

                return null;
            }
        }

        public bool ValidateClientConfig(InvocationScope invocationScope)
        {
            var scope = GetClientInvocationScope(invocationScope.ThreadId, invocationScope.InvocationScopeName);
            scope?.ValidateSettings();

            return true;
        }

        protected virtual void OnClientAdded(object sender, ClientArgs clientArgs)
        {
            ClientAdded?.Invoke(sender, clientArgs);
        }

        protected virtual void OnClientRemoved(object sender, ClientArgs clientArgs)
        {
            ClientRemoved?.Invoke(sender, clientArgs);
        }
    }
}
