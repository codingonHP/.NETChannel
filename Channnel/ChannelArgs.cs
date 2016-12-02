using System;

namespace Channnel
{
    public class ChannelArgs<T> : EventArgs
    {
        public T Data { get; set; }
        public string Name { get; set; }
        public string Operation { get; set; }
        public string SenderId { get; set; }
    }
}