using System;
using System.Collections.Generic;
using System.Threading;

namespace Channnel
{
    public class Channel<T> : IDisposable
    {
        #region Private
        private readonly int _buffer;
        private bool _debugInfo;

        private Queue<T> _dataPool = new Queue<T>();
        private readonly ChannelBehavior _channelBehavior;
        private readonly ChannelManager _channelManager = new ChannelManager();
        #endregion

        #region Public
        public string Name { get; set; }
        public bool DataAvailable { get; private set; }
        public bool ChannelOpen { get; private set; }
        public bool DebugInfo
        {
            get { return _debugInfo; }
            set
            {
                _debugInfo = value;
                if (_debugInfo)
                {
                    RegisterPrintDebugInfo();
                }
                else
                {
                    DeRegisterPrintDebugInfo();
                }
            }
        }
        #endregion

        #region Events
        public event Action<object, ChannelArgs<T>> DataWritten;
        public event Action<object, ChannelArgs<T>> DataRead;
        #endregion

        #region Constructor
        public Channel(string name, int buffer, ChannelBehavior channelBehavior, bool printDebugLogs)
        {
            _buffer = buffer;
            _channelBehavior = channelBehavior;

            DebugInfo = printDebugLogs;
            Name = name;
            ChannelOpen = true;

            if (_channelBehavior == ChannelBehavior.ExpandChannelOnNeed)
            {
                _buffer = -1;
            }
        }

        public Channel(ChannelConfig config) : this(config.Name,
                                                   config.Buffer,
                                                   config.ChannelBehavior,
                                                   config.PrintDebugLogs)
        {
           
        }

        public Channel() : this(name: string.Empty, buffer: 0, channelBehavior: ChannelBehavior.RemoveOnRead, printDebugLogs: false) { }

        public Channel(string name) : this(name: name, buffer: 0, channelBehavior: ChannelBehavior.RemoveOnRead, printDebugLogs: false) { }

        #endregion

        #region Methods
        public virtual T Read()
        {
            var thisClient = _channelManager.GetClientConfig(Thread.CurrentThread.ManagedThreadId.ToString());
            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (thisClient?.WriteOnly == true)
            {
                throw new InvalidOperationException("Cannot read from write only channel");
            }

            T data = default(T);
            while (!DataAvailable) { /*wait here till data is available */ }

            switch (_channelBehavior)
            {
                case ChannelBehavior.RemoveOnRead:
                    data = _dataPool.Dequeue();
                    break;

                case ChannelBehavior.RetainOnRead:
                    data = _dataPool.Peek();
                    break;
            }

            var channelArgs = new ChannelArgs<T>
            {
                Data = data,
                SenderId = Thread.CurrentThread.ManagedThreadId.ToString(),
                Operation = "Read",
                Name = Name
            };
            OnDataRead(this, channelArgs);

            return data;
        }

        public void Write(T data)
        {
            var thisClient = _channelManager.GetClientConfig(Thread.CurrentThread.ManagedThreadId.ToString());

            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (thisClient?.ReadOnly == true)
            {
                throw new InvalidOperationException("Cannot write to readonly channel");
            }

            if (CanWrite())
            {
                _dataPool.Enqueue(data);
                var channelArgs = new ChannelArgs<T>
                {
                    Data = data,
                    SenderId = Thread.CurrentThread.ManagedThreadId.ToString(),
                    Operation = "Write",
                    Name = Name
                };

                OnDataWritten(this, channelArgs);
            }
        }

        public virtual void Close()
        {
            ChannelOpen = false;
            Dispose();
        }

        public void RegisterClient(ClientConfig config)
        {
            Client client = new Client
            {
                ClientConfig = config,
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };

            _channelManager.AddNewClient(client);
        }

        public void DeRegisterClient()
        {
            Client client = new Client
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };

            _channelManager.RemoveClient(client);
        }

        public bool CanWrite()
        {
            if (_buffer == -1)
            {
                //infinite buffer size
                return true;
            }

            if (_buffer > 0)
            {
                return _dataPool.Count < _buffer;
            }

            return false;
        }

        protected virtual void OnDataWritten(object sender, ChannelArgs<T> channelArgs)
        {
            DataAvailable = _dataPool.Count > 0;
            DataWritten?.Invoke(sender, channelArgs);
        }

        protected virtual void OnDataRead(object sender, ChannelArgs<T> channelArgs)
        {
            DataAvailable = _dataPool.Count > 0;
            DataRead?.Invoke(sender, channelArgs);
        }

        public void Dispose()
        {
            _dataPool = null;
            DeRegisterPrintDebugInfo();
        }

        private void RegisterPrintDebugInfo()
        {
            DataWritten += (sender, args) =>
            {
                Console.WriteLine($"Data {args.Operation} by {args.Name} ({args.SenderId}), data: {args.Data}");
            };

            DataRead += (sender, args) =>
            {
                Console.WriteLine($"Data {args.Operation} by {args.Name} ({args.SenderId}), data: {args.Data}");
            };
        }

        private void DeRegisterPrintDebugInfo()
        {
            DataWritten = null;
            DataRead = null;
        }

        #endregion

    }
}
