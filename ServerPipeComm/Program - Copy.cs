using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerPipeComm
{
    class Program
    {
        private static NamedPipeServerStream myPipe;
        private static byte[] readBuffer;
        static void Main(string[] args)
        {
            Console.WriteLine(">>> Main");

            object readObject = new object();

            // Callback method
            AsyncCallback dataRead = new AsyncCallback(DataRead);

            readBuffer = new byte[256];

            try
            {
                // Create the named pipe for comm
                myPipe = new NamedPipeServerStream("myPipe.Comm", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
                
                    if (myPipe != null)
                    {
                        Console.WriteLine("Waiting for client connect");
                        myPipe.WaitForConnection();
                        Console.WriteLine("Client connected");

                        // Execution loop, only exits on exception right now or 
                        // if SDOHost is terminated
                        while (true)
                        {
                            // Once the connection is made start a read from the pipe and wait for a message to be recieved.
                            IAsyncResult readAsyncResult = myPipe.BeginRead(readBuffer, 0, 256, dataRead, readObject);

                            Console.WriteLine("Wait for read");

                            readAsyncResult.AsyncWaitHandle.WaitOne();
                            readAsyncResult.AsyncWaitHandle.Close();

                            Console.WriteLine("Read complete");

                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to create pipe");
                    }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("<<< Main");
        }

        private static void DataRead(IAsyncResult ar)
        {
            Console.WriteLine(">>> DataRead");

            try
            {
                if (ar.IsCompleted)
                {
                    // Terminate the read the read data from the buffer
                    int bytesRead = myPipe.EndRead(ar);

                    if (bytesRead > 0)
                    {
                        System.Text.ASCIIEncoding asciiEncoder = new System.Text.ASCIIEncoding();
                        string stringReadbuffer = asciiEncoder.GetString(readBuffer);
                        char[] trimChars = new char[2] { '\0', '\n' };
                        Console.WriteLine(stringReadbuffer.TrimEnd(trimChars));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // Create a new read buffer each loop so we have a fresh
            // read without including anything that was previously stored
            readBuffer = new byte[256];

            Console.WriteLine("<<< DataRead");
        }
    }
}
