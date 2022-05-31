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
            
            // WebsocketListener.AcceptClient()...
            TcpClient tcpClient = ((TcpListener)parameterz[0]).AcceptTcpClient();
            ChannelBridge channelBridge = (ChannelBridge) parameterz[1]; 
            INetworkStream networkStream = new NetworkStreamProxy(tcpClient.GetStream());
            WebsocketClient websocketClient = new WebsocketClient(networkStream, channelBridge);
            
            string remoteIp = GetRemoteIp(tcpClient);

            Console.WriteLine("A client connected from {0}", remoteIp);

            // DealWithWebsocketUpgrade()
            while (!tcpClient.Connected) ;
            while (!networkStream.DataAvailable) ; // block here till we have data
            while (tcpClient.Available < 2) ;      // Wait for the header bytes

            bool handshakSuccess = websocketClient.ReceiveHttpUpgradeRequest();

            // Handle ordinary websocket communication
            while (tcpClient.Connected)
            {
                Console.WriteLine("New Bytes ready for processing from client: " + tcpClient.Available);
                try
                {
                    WebsocketFrame websocketFrame = websocketClient.ReceiveMessageFromClient();
                    CommandRouter commandRouter = new CommandRouter(websocketClient);
                    commandRouter.HandleWebsocketMessage(websocketFrame);
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
