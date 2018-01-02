using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientPipeComm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(">>> Main");
            EMVConfigurationPipeServer pipeServer = new EMVConfigurationPipeServer();
            pipeServer.Start();
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            pipeServer.Write("Hello");
            while (true)
            { }
        }

    }
}
