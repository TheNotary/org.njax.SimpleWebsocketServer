using System.Net.Sockets;
using System.Text;
using System.IO;
using System;

namespace SimpleWebsocketServer
{
    public class TcpController
    {
        // Expects object[] { server, channelBridge };
        public static void HandleNewClientConnectionInThread(object? parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            object[] parameterz = (object[])parameters;

            TcpClient tcpClient = ((TcpListener)parameterz[0]).AcceptTcpClient();
            ChannelBridge channelBridge = (ChannelBridge) parameterz[1]; 
            INetworkStream networkStream = new NetworkStreamProxy(tcpClient.GetStream());
            WebsocketClient websocketClient = new WebsocketClient(networkStream, channelBridge);

            string remoteIp = GetRemoteIp(tcpClient);
            Console.WriteLine("A client connected from {0}", remoteIp);

            while (!tcpClient.Connected) ;
            while (tcpClient.Connected)
            {
                while (!networkStream.DataAvailable) ; // block here till we have data

                // wait for the first 2 bytes to be available.  Websocket messages consist of a two byte header detailing 
                // the shape of the incoming websocket frame...
                while (tcpClient.Available < 2) ;

                Console.WriteLine("New Bytes ready for processing from client: " + tcpClient.Available);
                string msg;

                try
                {
                    msg = HandleClientMessage(websocketClient, channelBridge);
                }
                catch (ClientClosedConnectionException ex)
                {
                    Console.WriteLine("Bytes in Frame were:\r\n" + networkStream.PrintBytesRecieved());
                    Console.WriteLine("  << Client Sent Close, dropping Stream >>\r\n" + ex.Message);
                    networkStream.SourceStream.Close();
                    tcpClient.Close();
                    tcpClient.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Bytes in Frame were:\r\n" + networkStream.PrintBytesRecieved());
                    Console.WriteLine("  << Exception encountered, closing client :p >>\r\n" + ex.Message);
                    networkStream.SourceStream.Close();
                    tcpClient.Close();
                    tcpClient.Dispose();
                    break;
                }
                Console.WriteLine("Bytes in Frame were:\r\n" + networkStream.PrintBytesRecieved());

                networkStream.ClearDebugBuffer();
            }
        }

        public static string HandleClientMessage(WebsocketClient websocketClient, ChannelBridge channelBridge)
        {
            INetworkStream networkStream = websocketClient.Stream;
            // Get the client's data now that they've at least gotten to the "GE" part of the HTTP upgrade request or the frame header.
            Byte[] headerBytes = new Byte[2];
            networkStream.Read(headerBytes, 0, headerBytes.Length);
            if (HandleHandshake(networkStream, headerBytes)) return "";

            // Handle ordinary websocket communication
            WebsocketFrame websocketFrame = websocketClient.ConsumeFrameFromStream(headerBytes);
            CommandRouter commandRouter = new CommandRouter(websocketClient);
            return commandRouter.HandleWebsocketMessage(websocketFrame);
        }

        public static bool HandleHandshake(INetworkStream stream, byte[] headerBytes)
        {
            String data = Encoding.UTF8.GetString(headerBytes);

            if (data != "GE")  // The handshake always begins with the line "GET " and websocket frames can't begin with G unless an extension was negotiated
                return false;

            HttpHandshaker handshaker = new HttpHandshaker(stream, headerBytes);
            handshaker.ConsumeHttpUpgradeRequestAndCollectWebsocketHeader();
            handshaker.RespondToHandshake();
            Console.WriteLine("Upgraded client to websockets.");
            return true;
        }

        public static byte[] BuildCloseFrame(byte[] closeCodeBytes)
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(0b10001000); // opcode for a finished, closed frame
            output.WriteByte(0x02);       // FIXME: this is a bug, assumed closeCodeBytes is size 2 and no masking of frame... length of close payload being 2, this message isn't masked, they say there's no vulnerability to the server...
            output.Write(closeCodeBytes, 0, closeCodeBytes.Length);
            return output.ToArray();
        }

        public static byte[] BuildCloseFrameClient()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(0b10001000); // opcode for a finished, closed frame
            output.WriteByte(0x00);
            return output.ToArray();
        }

        private static string GetRemoteIp(TcpClient tcpClient)
        {
            if (tcpClient == null || tcpClient.Client == null || tcpClient.Client.RemoteEndPoint == null || tcpClient.Client.RemoteEndPoint.ToString() == null)
                return "NONE";
            string? omg = tcpClient.Client.RemoteEndPoint.ToString();
            if (omg == null)
                return "NONE";
            return omg;
        }

    }
}
