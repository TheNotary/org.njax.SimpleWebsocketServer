using System.Net.Sockets;
using System.Text;
using System.IO;
using System;


namespace SimpleWebsocketServer
{
    public class NetworkStreamProxy : AbstractNetworkStreamProxy
    {
        private readonly NetworkStream _networkStream;
        public TcpClient? TcpClient;

        public NetworkStreamProxy(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public NetworkStreamProxy(TcpClient tcpClient)
        {
            _networkStream = tcpClient.GetStream();
            TcpClient = tcpClient;
        }

        public override bool DataAvailable => _networkStream.DataAvailable;
        public override Stream SourceStream => (Stream)_networkStream;
        public override Stream WriteStream => (Stream)_networkStream;
        public override MemoryStream ReadLog { get; set; } = new MemoryStream();

        public override int GetBytesAvailable() {
            if (TcpClient == null)
            { // in tests... we can't actually support this so we can just claim there's always 2 bytes available since in our tests we can guarantee that
                // though this could probably get smarter if needed
                return 2;
            }
            return TcpClient.Available;
        }


        public override string GetWritesAsString()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetWrites()
        {
            throw new NotImplementedException();
        }

    }
}
