using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorDeTurnos
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ShiftServer server = new ShiftServer();
            server.Init();

            Console.WriteLine(server.users.Length);

            Console.ReadLine();

        }
    }
}
