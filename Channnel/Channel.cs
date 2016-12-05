using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Channel;

namespace Channnel
{
    public class Channel<T> : IDisposable
    {
        #region Private
        private readonly int _buffer;
        private bool _debugInfo;
        private static readonly object Lock = new object();

        private Queue<T> _dataPool = new Queue<T>();
        private readonly ChannelBehavior _channelBehavior;
        private ChannelManager _channelManager = new ChannelManager();
        #endregion

        #region Public
        public string Name { get; set; }
        public string OwnerName { get; }
        public int ChannelThreadId { get; }
        public bool DataAvailable { get; private set; }
        public bool CanWrite { get; private set; }
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
                    UnRegisterPrintDebugInfo();
                }
            }
        }
        public List<Client> SubscriberList { get; private set; }
        #endregion

        #region Events
        public event Action<object, ChannelArgs<T>> DataWritten;
        public event Action<object, ChannelArgs<T>> DataRead;
        public event Action<object, ChannelArgs<T>> WaitingToWrite;
        public event Action<object, ChannelArgs<T>> WaitingToRead;

        #endregion

        #region Constructor
        public Channel(string name, int buffer, ChannelBehavior channelBehavior, bool printDebugLogs)
        {
            _buffer = buffer == 0 ? 1 : buffer;
            _channelBehavior = channelBehavior;

            OwnerName = Helpers.GetInvocationScopeMethodName(2);
            ChannelThreadId = Thread.CurrentThread.ManagedThreadId;
            DebugInfo = printDebugLogs;
            Name = name;
            ChannelOpen = true;
            SubscriberList = new List<Client>();

            if (_channelBehavior == ChannelBehavior.ExpandChannelOnNeed)
            {
                _buffer = -1;
            }
        }



        public Channel(ChannelConfig config) : this(config.ChannelName,
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
            var currentThread = Thread.CurrentThread;
            string invocationScopeName = Helpers.GetInvocationScopeMethodName(2);

            var thisClient = _channelManager.GetClientInvocationScope(currentThread.ManagedThreadId.ToString(), invocationScopeName);
            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (thisClient?.WriteOnly == true)
            {
                throw new InvalidOperationException($"Cannot read from write only channel from {invocationScopeName} invocation scope.");
            }

            T data = default(T);

            var channelArgs = new ChannelArgs<T>
            {
                SenderId = currentThread.ManagedThreadId.ToString(),
                Operation = ChannelOperation.Read,
                ChannelName = Name,
                InvocationScopeName = invocationScopeName
            };

            while (!DataAvailable)
            {
                /*wait here till data is available */
                OnWaitingToRead(this, channelArgs);
            }

            lock (Lock)
            {
                switch (_channelBehavior)
                {
                    case ChannelBehavior.RemoveOnRead:
                        data = _dataPool.Dequeue();
                        break;

                    case ChannelBehavior.RetainOnRead:
                        data = _dataPool.Peek();
                        break;
                }
            }

            channelArgs.Data = data;

            OnDataRead(this, channelArgs);
            CanWrite = true;

            return data;
        }

        public void Write(T data)
        {
            var currentThread = Thread.CurrentThread;
            string invocationScopeName = Helpers.GetInvocationScopeMethodName(2);

            var thisClient = _channelManager.GetClientInvocationScope(currentThread.ManagedThreadId.ToString(), invocationScopeName);

            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (thisClient?.ReadOnly == true)
            {
                throw new InvalidOperationException($"Cannot write to readonly channel from {invocationScopeName} invocation scope.");
            }

            var channelArgs = new ChannelArgs<T>
            {
                Data = data,
                SenderId = currentThread.ManagedThreadId.ToString(),
                Operation = ChannelOperation.Write,
                ChannelName = Name,
                InvocationScopeName = invocationScopeName
            };

            if (HaltTillWriteAllowed(channelArgs))
            {
                _dataPool.Enqueue(data);

                OnDataWritten(this, channelArgs);
            }
        }

        public virtual void Close()
        {
            if (Owner())
            {
                ChannelOpen = false;
                Dispose();
            }

            throw new InvalidOperationException("Cannot dispose channel from non-owner thread");
        }

        public void ConfigureChannelUse(InvocationScope invocationScope)
        {
            if (invocationScope != null)
            {
                invocationScope.ValidateSettings();
                invocationScope.ThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                Subscibe();
                _channelManager.AddNewInvocationScope(invocationScope);
            }
        }

        public void RemoveChannelConfiguration()
        {
            _channelManager.RemoveClient(Thread.CurrentThread.ManagedThreadId.ToString());
        }

        public void UnSubscribe()
        {
            if (AlreadySubscribed(3))
            {
                lock (Lock)
                {
                    var toRemove = SubscriberList.FirstOrDefault(s => s.ClientName.Equals(Helpers.GetInvocationScopeMethodName(4)));
                    SubscriberList.Remove(toRemove);
                    RemoveChannelConfiguration();
                }
            }
        }

        public void Subscibe()
        {
            lock (Lock)
            {
                if (!AlreadySubscribed(4))
                {
                    SubscriberList.Add(new Client());
                }
            }
        }

        public bool HaltTillWriteAllowed(ChannelArgs<T> channelArgs)
        {
            lock (Lock)
            {
                if (_buffer == -1)
                {
                    //infinite buffer size
                    return true;
                }

                if (_buffer > 0)
                {
                    var bufStatus = _dataPool.Count < _buffer;
                    if (bufStatus)
                    {
                        return true;
                    }
                }
            }

            while (!CanWrite)
            {
                /* wait till write operation is allowed. */
                OnWaitingToWrite(this, channelArgs);
            }

            HaltTillWriteAllowed(channelArgs);

            return false;
        }

        public void Dispose()
        {
            if (Owner())
            {
                _dataPool = null;
                _channelManager = null;
                UnRegisterPrintDebugInfo();
            }

            throw new InvalidOperationException("Cannot dispose channel from non-owner thread");
        }

        private void RegisterPrintDebugInfo()
        {
            DataWritten += (sender, args) =>
            {
                Console.WriteLine($"Data {args.Operation} by {args.ChannelName} ({args.SenderId}), data: {args.Data}");
            };

            DataRead += (sender, args) =>
            {
                Console.WriteLine($"Data {args.Operation} by {args.ChannelName} ({args.SenderId}), data: {args.Data}");
            };

            _channelManager.ClientAdded += (sender, args) =>
            {
                Console.WriteLine($"Client Added -> {args.InvocationScopes.Count}");
                foreach (var invocationScope in args.InvocationScopes)
                {
                    Console.WriteLine($"{invocationScope.InvocationScopeName} - { invocationScope.ThreadId }");
                }
            };

            _channelManager.ClientRemoved += (sender, args) =>
            {
                Console.WriteLine($"Client Removed -> {args.InvocationScopes.Count}");
                foreach (var invocationScope in args.InvocationScopes)
                {
                    Console.WriteLine($"{invocationScope.InvocationScopeName} - { invocationScope.ThreadId }");
                }
            };
        }

        private void UnRegisterPrintDebugInfo()
        {
            DataWritten = null;
            DataRead = null;
            _channelManager.ClientAdded -= null;
            _channelManager.ClientRemoved -= null;

        }

        private bool Owner()
        {
            return ChannelThreadId == Thread.CurrentThread.ManagedThreadId;
        }

        private bool AlreadySubscribed(int depth)
        {
            lock (Lock)
            {
                var lookUp = Helpers.GetInvocationScopeMethodName(depth);
                return SubscriberList.FirstOrDefault(s => s.ClientName.Equals(lookUp)) !=
                  null;
            }
        }
        #endregion

        #region EventInvocation

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

        protected virtual void OnWaitingToWrite(object sender, ChannelArgs<T> args)
        {
            args.Operation = ChannelOperation.WriteWait;
            WaitingToWrite?.Invoke(sender, args);
        }

        protected virtual void OnWaitingToRead(object sender, ChannelArgs<T> args)
        {
            args.Operation = ChannelOperation.ReadWait;
            WaitingToRead?.Invoke(sender, args);
        }

        #endregion
    }
}
