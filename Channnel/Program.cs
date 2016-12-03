using System;
using System.Threading.Tasks;

namespace Channnel
{
    class Program
    {
        static readonly Channel<int> OddChannel = new Channel<int>("odd");

        static readonly Channel<int> EvenChannel = new Channel<int>("even");

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
            oddChannel.RegisterClient(new ClientConfig { ReadOnly = true });
            evenChannel.RegisterClient(new ClientConfig { WriteOnly = true });

            var lastSavedData = 0;
            var nextData = oddChannel.Read();

            while (true)
            {
                nextData = lastSavedData + nextData;
                Console.WriteLine(nextData);
                lastSavedData = nextData;

                evenChannel.Write(nextData);
                nextData = oddChannel.Read();

                if (nextData > Limit)
                {
                    break;
                }
            }
        }

        private static void DoOddSum(Channel<int> oddChannel, Channel<int> evenChannel)
        {
            oddChannel.RegisterClient(new ClientConfig { WriteOnly = true });
            evenChannel.RegisterClient(new ClientConfig { ReadOnly = true });

            var nextData = 1;
            while (true)
            {
                Console.WriteLine(nextData);

                oddChannel.Write(nextData);
                var lastSavedData = nextData;

                nextData = evenChannel.Read();
                nextData = lastSavedData + nextData;


                if (nextData > Limit)
                {
                    break;
                }
            }
        }
    }
}
