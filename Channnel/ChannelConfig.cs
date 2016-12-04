namespace Channnel
{
    public class ChannelConfig
    {
        public string ChannelName { get; set; }
        public int Buffer { get; set; }
        public bool PrintDebugLogs { get; set; }
        public ChannelBehavior ChannelBehavior { get; set; }
    }
}
