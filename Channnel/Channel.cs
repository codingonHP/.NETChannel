using System;
using System.Collections.Generic;

namespace Channnel
{
    public enum ChannelBehavior
    {
        RemoveOnRead,
        RetainOnRead,
        ExpandChannelOnNeed
    }

    public class Channel<T> : IDisposable
    {
        private readonly int _buffer;
        private readonly ChannelBehavior _channelBehavior;
        private Queue<T> _dataPool = new Queue<T>();

        public event Action<object, ChannelArgs<T>> DataWritten;
        public event Action<object, ChannelArgs<T>> DataRead;
        public bool DataAvailable { get; private set; }
        public bool ChannelOpen { get; private set; }

        public Channel(int buffer, ChannelBehavior channelBehavior)
        {
            _buffer = buffer;
            _channelBehavior = channelBehavior;
            ChannelOpen = true;

            if (_channelBehavior == ChannelBehavior.ExpandChannelOnNeed)
            {
                _buffer = -1;
            }
        }

        public Channel() : this(ChannelBehavior.RemoveOnRead) { }

        public Channel(ChannelBehavior channelBehavior): this(1, channelBehavior) { }

        public Channel(int buffer) : this(buffer,ChannelBehavior.RemoveOnRead) { }
      

        public void Write(T data)
        {
            if (!ChannelOpen)
            {
                throw new InvalidOperationException("Reading from closed channel");
            }

            if (CanWrite())
            {
                _dataPool.Enqueue(data);
                OnDataWritten(this, new ChannelArgs<T> { Data = data });
            }
        }

        private bool CanWrite()
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

            OnDataRead(this, new ChannelArgs<T> { Data = data });

            return data;
        }

        public void Close()
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
        }
    }

    public class ChannelArgs<T> : EventArgs
    {
        public T Data { get; set; }
        public Guid SenderId { get; set; }
    }
}
