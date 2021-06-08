using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBCommTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var scu = new SensorControlUnit() { PortName = "COM3" };
            while (true)
            {
                Console.WriteLine("Sending ReadySignal");
                scu.SendReadySignal();
                Console.Write("Awaiting response");
                var lastResponse = scu.LastResp;
                while (scu.WaitingDataReceived > 0)
                {
                    lastResponse = scu.LastResp;
                }
                Console.WriteLine("");
                Console.Write("Response: ");
                Console.WriteLine(lastResponse);
                Console.WriteLine("Ready to run validation process");
                Console.ReadKey();
                Console.WriteLine("Validation process Succeeded");
                Console.WriteLine("Sending SuccessSignal");
                scu.SendSuccessSignal();
                Console.Write("Awaiting response");
                while (scu.WaitingDataReceived > 0)
                {
                    lastResponse = scu.LastResp;
                }
                Console.WriteLine("");
                Console.Write("Response: ");
                Console.WriteLine(lastResponse);
                Console.ReadKey();
            }
        }
    }
}
