using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;

namespace ClientPipeComm
{
    internal class FepClient
    {
        private readonly NamedPipeClientStream fepClient;
        private readonly string pipeName;
        public FepClient()
        {
            pipeName = "PipeLis";
            fepClient = new NamedPipeClientStream("PipeLis");
        }

        /// <summary>
        /// Starts the client. Connects to the server.
        /// </summary>
        public void Start()
        {            
            const int tryConnectTimeout = 5 * 60 * 1000; // 5 minutes
            fepClient?.Connect(tryConnectTimeout);
        }
        public Task<TaskResult> SendMessage(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            if (fepClient.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                fepClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
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
            fepClient.EndWrite(asyncResult);
            fepClient.Flush();

            return new TaskResult { IsSuccess = true };
        }

        public void Stop()
        {
            try
            {
                fepClient.WaitForPipeDrain();
            }
            finally
            {
                fepClient.Close();
                fepClient.Dispose();
            }
        }
    }

    public class TaskResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }
}
