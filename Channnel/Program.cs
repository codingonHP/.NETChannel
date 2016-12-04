using System;
using System.Threading.Tasks;

namespace Channnel
{
    class Program
    {
        static readonly Channel<int> OddChannel = new Channel<int>(new ChannelConfig { ChannelName = "odd", PrintDebugLogs = false });

        static readonly Channel<int> EvenChannel = new Channel<int>(new ChannelConfig { ChannelName = "even", PrintDebugLogs = false });

        private const int Limit = 7000;

        /// <summary>
        /// Printing fib series using channel
        /// </summary>
        static void Main()
        {

            Task.Factory.StartNew(() =>
            {
                DoOddSum(OddChannel, EvenChannel);
            });

            Task.Factory.StartNew(() =>
            {
                DoEvenSum(OddChannel, EvenChannel);
            });

            Console.ReadKey();

        }

        private static void DoEvenSum(Channel<int> oddChannel, Channel<int> evenChannel)
        {
            oddChannel.ConfigureChannelUse(new InvocationScope { InvocationScopeName = "DoEvenSum", ReadOnly = true });
            evenChannel.ConfigureChannelUse(new InvocationScope { InvocationScopeName = "DoEvenSum", WriteOnly = true });

            var lastSavedData = 0;
            var nextData = oddChannel.Read("DoEvenSum");

            while (true)
            {
                nextData = lastSavedData + nextData;
                Console.WriteLine(nextData);
                lastSavedData = nextData;

                evenChannel.Write(nextData, "DoEvenSum");
                nextData = oddChannel.Read("DoEvenSum");

                if (nextData > Limit)
                {
                    break;
                }


                //testing code
                //oddChannel.Write(nextData, "DoEvenSum");
                //nextData = evenChannel.Read("DoEvenSum");

            }
        }

        private static void DoOddSum(Channel<int> oddChannel, Channel<int> evenChannel)
        {
            oddChannel.ConfigureChannelUse(new InvocationScope { InvocationScopeName = "DoOddSum", WriteOnly = true});
            evenChannel.ConfigureChannelUse(new InvocationScope { InvocationScopeName = "DoOddSum", ReadOnly = true });

            var nextData = 1;
            while (true)
            {
                Console.WriteLine(nextData);

                oddChannel.Write(nextData, "DoOddSum");
                var lastSavedData = nextData;

                nextData = evenChannel.Read("DoOddSum");
                nextData = lastSavedData + nextData;


                if (nextData > Limit)
                {
                    break;
                }

                //testing code
                //oddChannel.Read("DoOddSum");
                //evenChannel.Write(nextData, "DoOddSum");
            }
        }
    }
}
