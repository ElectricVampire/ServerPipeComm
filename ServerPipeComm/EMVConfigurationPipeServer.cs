using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientPipeComm
{
    class EMVConfigurationPipeServer
    {
        private readonly string pipeName;
        private InternalServer server;
        private FepClient client;
        public EMVConfigurationPipeServer()
        {
            pipeName = "myPipe.Comm";
            client = new FepClient();
        }

        public void Start()
        {
            StartNamedPipeServer();
            client.Start();
        }
        
        private void StartNamedPipeServer()
        {
            server = new InternalServer(pipeName, NamedPipeServerStream.MaxAllowedServerInstances);
            server.ClientConnectedEvent += ClientConnectedHandler;
            server.ClientDisconnectedEvent += ClientDisconnectedHandler;
            server.MessageReceivedEvent += MessageReceivedHandler;
            server.Start();
        }

        internal void Write(string message)
        {
            server.SendMessage(message);
        }
        public void Stop()
        {
            Console.WriteLine(">>> EMVConfigPS: Stop");
            try
            {
                UnregisterFromServerEvents(server);
                server.Stop();
                client.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error ::Failed to stop server" + ex.Message);
            }
            Console.WriteLine("<<< EMVConfigPS: Stop");
        }

        private void StopNamedPipeServer()
        {
            Console.WriteLine(">>> EMVConfigPS: StopNamedPipeServer");
            UnregisterFromServerEvents(server);
            Console.WriteLine("<<< EMVConfigPS: StopNamedPipeServer");
        }

        private void UnregisterFromServerEvents(InternalServer server)
        {
            Console.WriteLine(">>> EMVConfigPS: UnregisterFromServerEvents");

            server.ClientConnectedEvent -= ClientConnectedHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedHandler;
            server.MessageReceivedEvent -= MessageReceivedHandler;

            Console.WriteLine("<<< EMVConfigPS: UnregisterFromServerEvents");
        }

        private void ClientConnectedHandler(object sender, object eventArg)
        {
            Console.WriteLine("Client Connected");
            
        }

        private void ClientDisconnectedHandler(object sender, object eventArg)
        {
            Console.WriteLine("High probability that client is not alive or connected");
            UnregisterFromServerEvents(server);
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine("Message :"+ eventArgs.Message);
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
