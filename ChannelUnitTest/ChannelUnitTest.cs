using System.Threading;
using System.Threading.Tasks;
using Channnel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChannelUnitTest
{
    [TestClass]
    public class ChannelUnitTest
    {
        static readonly Channel<int> OddChannel = new Channel<int>(new ChannelConfig { ChannelName = "odd", Buffer = 5});
        static readonly Channel<int> EvenChannel = new Channel<int>(new ChannelConfig { ChannelName = "even", Buffer = 5});

        [TestMethod]
        public void ChannelWithBuffer()
        {
            int aSenderId = 0;
            int bSenderId;

            OddChannel.WaitingToWrite += (o, args) =>
            {
                Assert.AreEqual(args.Operation, ChannelOperation.WriteWait);
                Assert.AreEqual(args.Data, 5);
                Assert.AreEqual(args.ChannelName, "odd");
                Assert.AreEqual(args.InvocationScopeName, "A");
                Assert.AreEqual(args.SenderId, aSenderId.ToString());
            };

            Task.Factory.StartNew(() =>
            {
                aSenderId = Thread.CurrentThread.ManagedThreadId;
                A();
            });

            Task.Factory.StartNew(() =>
            {
                bSenderId = Thread.CurrentThread.ManagedThreadId;
                B();
            });

        }

        private void B()
        {
            OddChannel.Write(0, "B");
            OddChannel.Write(2, "B");
            OddChannel.Write(4, "B");
        }

        private void A()
        {
            OddChannel.Write(1, "A");
            OddChannel.Write(3, "A");

            //wait to write 5
            OddChannel.Write(5, "A");
        }
    }
}
