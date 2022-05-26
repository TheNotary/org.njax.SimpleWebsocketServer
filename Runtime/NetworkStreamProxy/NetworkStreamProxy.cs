using System.Net.Sockets;
using System.Text;


namespace SimpleWebsocketServer
{
    public class NetworkStreamProxy : AbstractNetworkStreamProxy
    {
        private readonly NetworkStream _networkStream;

        public NetworkStreamProxy(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public override bool DataAvailable => _networkStream.DataAvailable;
        public override Stream SourceStream => (Stream)_networkStream;
        public override Stream WriteStream => (Stream)_networkStream;
        public override MemoryStream ReadLog { get; set; } = new MemoryStream();


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
