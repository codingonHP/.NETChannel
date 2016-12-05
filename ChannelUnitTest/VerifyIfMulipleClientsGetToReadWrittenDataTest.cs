using System.Threading;
using System.Threading.Tasks;
using Channnel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChannelUnitTest
{
    [TestClass]
    public class VerifyIfMulipleClientsGetToReadWrittenDataTest
    {
        static readonly Channel<int> OddChannel = new Channel<int>(new ChannelConfig { ChannelName = "odd", Buffer = 5 });

        [TestMethod]
        public void VerifyIfMulipleClinetsGetToReadWrittenData()
        {
            int b = 0, c = 0, d = 0;
            Task.Factory.StartNew(() =>
            {
                b = B();
            });

            Task.Factory.StartNew(() =>
            {
               c = C();
            });

            Task.Factory.StartNew(() =>
            {
                d = D();
            });

            Thread.Sleep(5000);
            Task.Factory.StartNew(() =>
            {
                A();
            });

            Thread.Sleep(5000);

            Assert.AreEqual(1, b,$"expected 1 but got {b}");
            Assert.AreEqual(1, c,$"expected 1 but got {c}");
            Assert.AreEqual(1, d, $"expected 1 but got {d}");
        }

        private int B()
        {
            var read = OddChannel.Read();
            return read;
        }

        private int C()
        {
            var read = OddChannel.Read();
            return read;
        }

        private int D()
        {
            var read = OddChannel.Read();
            return read;
        }

        private void A()
        {
            OddChannel.Write(1);
        }
    }
}
