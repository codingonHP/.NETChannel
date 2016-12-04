using System;

namespace Channnel
{
    public class ChannelArgs<T> : EventArgs
    {
        public T Data { get; set; }
        public string ChannelName { get; set; }
        public ChannelOperation Operation { get; set; }
        public string SenderId { get; set; }
        public string InvocationScopeName { get; set; }
    }
}