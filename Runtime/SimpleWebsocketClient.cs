using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SimpleWebsocketServer
{
    internal class SimpleWebsocketClient
    {
        private string localAddress;
        private int port;
        private string websocketToken;
        private TcpListener? server;
        private LinkedList<Thread> threads = new LinkedList<Thread>();

        public NetworkStreamProxy? networkStream;
        public TcpClient? tcpClient;
        public WebsocketClient? websocketClient;

        public SimpleWebsocketClient(string localAddress, int port)
        {
            this.localAddress = localAddress;
            this.port = port;
            this.websocketToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(GenerateRandomPassword()));
        }

        public void Handshake()
        {
            tcpClient = new TcpClient(this.localAddress, this.port);
            networkStream = new NetworkStreamProxy(tcpClient.GetStream());
            websocketClient = new WebsocketClient(networkStream);

            while (!tcpClient.Connected) ;   // Block until connected

            // perform handshake...
            string handshake = $"GET / HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nSec-WebSocket-Key: ${websocketToken}\r\n\r\n";
            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshake);
            networkStream.Write(handshakeBytes, 0, handshakeBytes.Length);

            // Consume handshake response
            ConsumeHandshakeResponse(networkStream);
        }

        internal void SendCloseFrame()
        {
            byte[] closeFrame = TcpController.BuildCloseFrameClient();
            networkStream.Write(closeFrame, 0, closeFrame.Length);
        }

        public void SendMessage(string v)
        {
            websocketClient.SendMessage(v, true);
        }

        private void ConsumeHandshakeResponse(NetworkStreamProxy networkStream)
        {
            NetworkStreamReader sr = new NetworkStreamReader(networkStream);

            string debug = "";
            string line;
            while (true)  // TODO: implement a receive timeout
            {
                line = sr.ReadUntilCarriageReturn();
                debug += line + "\r\n";
                if (line == "") break;  // EOF reached
            }

            // ValidateThatThisIsReallyAValidResponse()
        }

        internal static string GenerateRandomPassword()
        {
            int tokenLength = 10;

            char[] charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&()".ToCharArray();
            int byteSize = 256; //Labelling convenience
            int biasZone = byteSize - (byteSize % charSet.Length);

            byte[] rBytes = new byte[tokenLength]; //Do as much before and after lock as possible
            char[] rName = new char[tokenLength];

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(rBytes);
            for (var i = 0; i < tokenLength; i++)
            {
                rName[i] = charSet[rBytes[i] % charSet.Length];
            }

            return new string(rName);
            //return "password";
        }
    }
}