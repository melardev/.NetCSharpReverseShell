using System.Net;

namespace ReverseShell
{
    interface INetService
    {
        void Start(IPAddress ipAddress, int port);
        void WriteLine(string output);
        void InteractAsync();
        void ReadSync();
        void Write(string output);
        void Shutdown();
    }
}