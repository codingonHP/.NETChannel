﻿using System;
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
        private ChannelManager _channelManager = new ChannelManager();
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
                    UnRegisterPrintDebugInfo();
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
            _buffer = buffer == 0 ? 1 : buffer;
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
        public virtual T Read(string invocationScopeName)
        {
            var currentThread = Thread.CurrentThread;
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
                SenderId = currentThread.ManagedThreadId.ToString(),
                Operation = "Read",
                Name = Name
            };
            OnDataRead(this, channelArgs);

            return data;
        }

        public void Write(T data, string invocationScopeName)
        {
            var currentThread = Thread.CurrentThread;
            var thisClient = _channelManager.GetClientInvocationScope(currentThread.ManagedThreadId.ToString(), invocationScopeName);

            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (thisClient?.ReadOnly == true)
            {
                throw new InvalidOperationException($"Cannot write to readonly channel from {invocationScopeName} invocation scope.");
            }

            if (CanWrite())
            {
                _dataPool.Enqueue(data);
                var channelArgs = new ChannelArgs<T>
                {
                    Data = data,
                    SenderId = currentThread.ManagedThreadId.ToString(),
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

        public void ConfigureChannelUse(InvocationScope invocationScope)
        {
            invocationScope.ValidateSettings();
            invocationScope.ThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
            _channelManager.AddNewInvocationScope(invocationScope);
        }

        public void RemoveChannelConfiguration()
        {
            _channelManager.RemoveClient(Thread.CurrentThread.ManagedThreadId.ToString());
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
            _channelManager = null;
            UnRegisterPrintDebugInfo();
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

        #endregion

    }
}
