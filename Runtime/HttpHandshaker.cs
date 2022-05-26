using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebsocketServer
{
    public class HttpHandshaker
    {
        private INetworkStream stream;
        private byte[] headerBytes;
        private NetworkStreamReader sr;
        public string inboundWebSocketKey = "";
        public string requestedResource = "";
        public string responseWebsocketHeaderValue = "";

        public HttpHandshaker(INetworkStream stream, byte[] headerBytes)
        {
            this.stream = stream;
            this.headerBytes = headerBytes;
            this.sr = new NetworkStreamReader(stream);
        }

        /// <summary>
        /// This function parses a stream until it gets to a blank line (/r/n/r/n) meaning the end 
        /// of their opening HTTP upgrade request.  As side effects it will populate the 
        /// /inboundWebSocketKey/ and /requestedResource/ fields.  
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// If the parser is unable to collect a valid inboundWebSocketKey from the HTTP request 
        /// it will throw an exception. 
        /// </exception>
        public void ConsumeHttpUpgradeRequestAndCollectWebsocketHeader()
        {
            string websocketKey = "Sec-WebSocket-Key:";
            string resourceRequestedLine = "GET /";
            string debug = "GE";
            string line;
            bool processedFirstLine = false;
            while (true)  // TODO: implement a receive timeout
            {
                line = sr.ReadUntilCarriageReturn();
                debug += line + "\r\n";
                if (line == "") break;  // EOF reached

                if (!processedFirstLine)  // ProcessFirstLine()
                {
                    string firstLine = "GE" + line;
                    if (firstLine.Length >= 5 &&
                        firstLine.Substring(0, 5) == resourceRequestedLine)
                    {
                        line = "GE" + line;

                        requestedResource = line
                            .Replace("GET /", "/")
                            .Replace(" HTTP/1.1", "");
                    }
                    processedFirstLine = true;
                }

                // handle extracting websocket key
                if (line.StartsWith(websocketKey))
                {
                    inboundWebSocketKey = line.Substring(websocketKey.Length).Trim();
                }

                // check if we've got a double /r/n
                if (line == "") // if we're out of data and we received an empty line
                    break;
            }

            responseWebsocketHeaderValue = GenerateResponseWebsocketHeaderValue();

            var debugMessage = "Requested Resource: " + requestedResource + "\r\n"
                             + "Inbound Websocket Key: " + inboundWebSocketKey + "\r\n"
                             + "Response Websocket Header Value: " + responseWebsocketHeaderValue + "\r\n"
                             + "Handshake Request: \r\n" + debug;
            Console.WriteLine(debugMessage);

            // ValidateThatThisIsReallyAValidWebsocketUpgradeRequest()
            if (inboundWebSocketKey == "")
                throw new NotSupportedException("could not extract websocket header from handshake.  Wrong number?");

        }

        private string GenerateResponseWebsocketHeaderValue()
        {
            if (inboundWebSocketKey == "")
            {
                throw new InvalidOperationException("GenerateResponseWebsocketHeaderValue was called when no inboundWebSocketKey was available.");
            }
            return Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(inboundWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        }

        public void RespondToHandshake()
        {
            const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                + "Connection: Upgrade" + eol
                + "Upgrade: websocket" + eol
                + "Sec-WebSocket-Accept: " + responseWebsocketHeaderValue + eol
                + eol);
            stream.Write(response, 0, response.Length);
        }
    }
}
