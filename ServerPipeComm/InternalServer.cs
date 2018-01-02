using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientPipeComm
{
    internal class InternalServer
    {
        private readonly NamedPipeServerStream pipeServer;
        private bool isStopping;
        private readonly object lockingObject = new object();
        private const int BufferSize = 256;

        public event EventHandler ClientConnectedEvent;
        public event EventHandler ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        public Task<TaskResult> SendMessage(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            if (pipeServer.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                pipeServer.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(EndWriteCallBack(asyncResult));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }

                }, null);
            }
            else
            {
                Console.WriteLine("Cannot send message, pipe is not connected");
                throw new IOException("pipe is not connected");
            }

            return taskCompletionSource.Task;
        }

        private TaskResult EndWriteCallBack(IAsyncResult asyncResult)
        {
            pipeServer.EndWrite(asyncResult);
            pipeServer.Flush();

            return new TaskResult { IsSuccess = true };
        }

        //public void Stop()
        //{
        //    try
        //    {
        //        pipeServer.WaitForPipeDrain();
        //    }
        //    finally
        //    {
        //        pipeServer.Close();
        //        pipeServer.Dispose();
        //    }
        //}



        private class Message
        {
            public readonly byte[] Buffer;
            public readonly StringBuilder StringBuilder;

            public Message()
            {
                Buffer = new byte[BufferSize];
                StringBuilder = new StringBuilder();
            }
        }


        public InternalServer(string pipeName, int maxNumberOfServerInstances)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        }

        public void Start()
        {
            Console.WriteLine(">>> IS :Start");
            try
            {
                Console.WriteLine("Waiting for client");
                pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}", ex.Message);
                throw;
            }
            Console.WriteLine("<<< IS :Start");
        }

        public void Stop()
        {
            Console.WriteLine(">>> IS :Stop");
            isStopping = true;

            try
            {
                if (pipeServer.IsConnected)
                {
                    Console.WriteLine("Pipe disconnected");
                    pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stop ::Exception : {0}", ex.Message);
                throw;
            }
            finally
            {
                Console.WriteLine("Pipe disposed");
                pipeServer.Close();
                pipeServer.Dispose();
            }
            Console.WriteLine("<<< IS :Stop");
        }


        private void BeginRead(Message info)
        {
            Console.WriteLine(">>> IS :BeginRead ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
            try
            {
                pipeServer.BeginRead(info.Buffer, 0, BufferSize, EndReadCallBack, info);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}", ex.Message);
                throw;
            }
            Console.WriteLine("<<< IS :BeginRead ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
        }

        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            Console.WriteLine(">>> IS :WaitForConnectionCallBack");
            if (!isStopping)
            {
                lock (lockingObject)
                {
                    Console.WriteLine("Inside lock");
                    if (!isStopping)
                    {
                        // Call EndWaitForConnection to complete the connection operation
                        pipeServer.EndWaitForConnection(result);
                        OnConnected();
                        BeginRead(new Message());
                    }
                }
            }
            Console.WriteLine("<<< IS :WaitForConnectionCallBack");
        }

        internal void Write(string message)
        {
            lock (lockingObject)
            {
                byte[] writeBuffer = Encoding.ASCII.GetBytes(message);
                pipeServer.Write(writeBuffer, 0, writeBuffer.Length);
            }
        }
        private void EndReadCallBack(IAsyncResult result)
        {
            Console.WriteLine(">>> IS :EndReadCallBack ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
            var readBytes = pipeServer.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Message)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!pipeServer.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(info);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = info.StringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginRead(new Message());
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
                Console.WriteLine("ReadBytes are less then or equal to 0");
                if (!isStopping)
                {
                    lock (lockingObject)
                    {
                        if (!isStopping)
                        {
                            OnDisconnected();
                            Stop();
                        }
                    }
                }
            }

            Console.WriteLine("<<< IS :EndReadCallBack ThreadId: {0}", Thread.CurrentThread.ManagedThreadId);

        }

        /// <summary>
        /// This method fires MessageReceivedEvent with the given message
        /// </summary>
        private void OnMessageReceived(string message)
        {
            MessageReceivedEvent?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        /// <summary>
        /// This method fires ConnectedEvent 
        /// </summary>
        private void OnConnected()
        {
            ClientConnectedEvent?.Invoke(this, null);
        }

        /// <summary>
        /// This method fires DisconnectedEvent 
        /// </summary>
        private void OnDisconnected()
        {
            ClientDisconnectedEvent?.Invoke(this, null);
        }
        public class TaskResult
        {
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
    
}
