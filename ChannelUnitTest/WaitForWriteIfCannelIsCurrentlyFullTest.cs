using System.Threading;
using System.Threading.Tasks;
using Channnel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChannelUnitTest
{
    [TestClass]
    public class WaitForWriteIfCannelIsCurrentlyFullTest
    {
        static readonly Channel<int> OddChannel = new Channel<int>(new ChannelConfig { ChannelName = "odd", Buffer = 5});

        [TestMethod]
        public void WaitForWriteIfCannelIsCurrentlyFull()
        {
            int aSenderId = 0;

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
                B();
            });

        }

        private void B()
        {
            OddChannel.Write(0);
            OddChannel.Write(2);
            OddChannel.Write(4);
        }

        private void A()
        {
            OddChannel.Write(1);
            OddChannel.Write(3);

            //wait to write 5
            OddChannel.Write(5);
        }
    }
}
