using System.Text;
using System.Text.RegularExpressions;

namespace SimpleWebsocketServer
{
    public class CommandRouter
    {
        WebsocketClient _websocketClient;

        public CommandRouter(WebsocketClient websocketClient)
        {
            _websocketClient = websocketClient;
        }

        public string HandleWebsocketMessage(WebsocketFrame websocketFrame)
        {
            INetworkStream stream = _websocketClient.Stream;

            if (!websocketFrame.isMasked && websocketFrame.payloadLength != 0)
                throw new NotSupportedException("mask bit not set.  Masks MUST be set by the client when sending messages to prevent cache poisoning attacks leveraged against internet infrastructure like proxies and cyber warfar appliances.");

            switch (websocketFrame.opcode)
            {
                case (0x01):  // text message
                    string msg = Encoding.UTF8.GetString(websocketFrame.cleartextPayload);
                    Console.WriteLine("> Client: {0}", msg);
                    HandleCommand(msg);
                    return msg;
                case (0x08):  // close message
                    byte[] response = _websocketClient.BuildCloseFrame(websocketFrame.cleartextPayload);
                    stream.Write(response, 0, response.Length);
                    string closeCodeString = websocketFrame.closeCode != 0
                        ? "Close frame code was " + websocketFrame.closeCode
                        : "There was no close code.";
                    throw new ClientClosedConnectionException("The client sent a close frame.  " + closeCodeString);
                default:
                    Console.WriteLine("Unknown websocket Opcode received from client: {0}", websocketFrame.opcode);
                    return "";
                    throw new NotSupportedException();
            }
        }

        public void HandleCommand(string msg)
        {
            if (msg == "" || msg.Substring(0, 1) != "/")
                return; // This is not a command message

            string command = Regex.Match(msg, "^/(\\w+)").Value;

            switch (command)
            {
                case ("/close"):
                case ("/c"):
                    if (_websocketClient.AdminAuthenticated)
                        CloseServer();
                    else
                        Console.WriteLine("Not admin, close server request denied");
                    break;
                case ("/auth"):
                case ("/a"):
                    AuthenticateClient(msg);
                    break;
                case ("/subscribe"):
                case ("/s"):
                    SubscribeToChannel(msg);
                    break;
                case ("/publish"):
                case ("/p"):
                    PublishToChannel(msg);
                    break;
                default:
                    Console.WriteLine("Client sent unknown command");
                    break;
            }
        }

        /// <summary>
        /// Use this method to gain "admin" permissions using a password stored in the env.
        /// </summary>
        /// <param name="msg"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AuthenticateClient(string msg)
        {
            string cleanMsg = StripCommand(msg);
            string adminPassword = _websocketClient.channelBridge.adminPassword;

            if (adminPassword == cleanMsg)
            { 
                _websocketClient.AdminAuthenticated = true;
                _websocketClient.SendMessage("OK");
                Console.WriteLine("Admin Password checked to authenticate client");
                return;
            }

            Console.WriteLine("Authentication failure, wrong password");
        }


        private void CloseServer()
        {
            Console.WriteLine("Client sent close command, closing.");
            _websocketClient.channelBridge.ManagementCancelationToken.Cancel();
            //Environment.Exit(0);
        }

        private void SubscribeToChannel(string msg)
        {
            string cleanMsg = StripCommand(msg);
            string channelName = cleanMsg;

            _websocketClient.Subscribe(_websocketClient.channelBridge, channelName);
        }

        private void PublishToChannel(string msg)
        {
            // split message into parameters
            string cleanMsg = StripCommand(msg);
            string channelName = cleanMsg.Split(" ")[0];
            string content = Regex.Replace(cleanMsg, $"^{channelName}", "").Trim();

            // send message to all client subscribing to that message
            _websocketClient.channelBridge.PublishContent(channelName, content);
        }
        private string StripCommand(string msg)
        {
            string cmdName = msg.Split(" ")[0].Replace("/", "");
            return Regex.Replace(msg, $"^/{cmdName}", "").Trim();
        }

    }
}
