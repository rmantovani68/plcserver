using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plcserver
{
    class Program
    {
        public static void Main()
        {
            plcserver PlcServer = new plcserver();
            
            Console.WriteLine("PLCServer has started.");
            Console.WriteLine("Press enter to stop...");
            Console.ReadLine();

            PlcServer.Exit();

        }
    }
}
