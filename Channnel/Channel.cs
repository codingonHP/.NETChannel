using System;
using System.Collections.Generic;
using System.Threading;

namespace Channnel
{
    public class Channel<T> : IDisposable
    {
        #region Private
        private readonly int _buffer;
        private readonly ChannelBehavior _channelBehavior;
        private Queue<T> _dataPool = new Queue<T>();
        #endregion

        #region Public
        public string Name { get; set; }
        public bool DataAvailable { get; private set; }
        public bool ChannelOpen { get; private set; }
        public bool DebugInfo { get; }
        #endregion

        #region Events
        public event Action<object, ChannelArgs<T>> DataWritten;
        public event Action<object, ChannelArgs<T>> DataRead;
        #endregion

        #region Constructor
        public Channel(int buffer, ChannelBehavior channelBehavior, bool printDebugLogs, string name)
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

            if (DebugInfo)
            {
                PrintDebugInfo();
            }
        }
        public Channel(int buffer, ChannelBehavior channelBehavior, bool printDebugLogs) : this(buffer, channelBehavior, printDebugLogs, string.Empty) { }
        public Channel() : this(ChannelBehavior.RemoveOnRead) { }
        public Channel(bool printDebugInfo) : this(1, ChannelBehavior.RemoveOnRead, printDebugInfo) { }
        public Channel(ChannelBehavior channelBehavior) : this(1, channelBehavior, false) { }
        public Channel(int buffer) : this(buffer, ChannelBehavior.RemoveOnRead, false) { }
        public Channel(int buffer, string name) : this(buffer, ChannelBehavior.RemoveOnRead, false, name) { }
        public Channel(string name) : this(1, ChannelBehavior.RemoveOnRead, false, name) { }
        public Channel(string name, bool printDebugInfo) : this(1, ChannelBehavior.RemoveOnRead, printDebugInfo, name) { }
        public Channel(ChannelBehavior channelBehavior, bool printDebugLogs, string name) : this(1, channelBehavior, printDebugLogs, name) { }
        public Channel(int buffer, bool printDebugLogs, string name) : this(buffer, ChannelBehavior.RemoveOnRead, printDebugLogs, name) { }



        #endregion

        #region Methods
        public void Write(T data)
        {
            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
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

        public virtual T Read()
        {
            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
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

        public virtual void Close()
        {
            ChannelOpen = false;
            Dispose();
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
            DataWritten = null;
            DataRead = null;
        }

        private void PrintDebugInfo()
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


        #endregion

    }
}
