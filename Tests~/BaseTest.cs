using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleWebsocketServer;
using Xunit.Abstractions;

namespace SimpleWebsocketServerTest
{
    public class BaseTest
    {
        string validHttpUpgradeRequest = $"GET / HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nSec-WebSocket-Key: zzz\r\n\r\n";
        byte[] validWebsocketHello = new byte[] { 129, 133, 90, 120, 149, 83, 50, 29, 249, 63, 53 };
        byte[] validClientClose = new byte[] { 136, 130, 104, 40, 78, 91, 107, 193 };
        string validHandshakeResponse = "HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: EJ5xejuUCHQkIKE2QxDTDCDws8Q=\r\n\r\n";

        public WebsocketClient CreateWebsocketClient(string streamString)
        {
            byte[] streamBytes = Encoding.UTF8.GetBytes(streamString);
            ChannelBridge cb = new ChannelBridge("");
            MockNetworkStreamProxy networkStreamProxy = new MockNetworkStreamProxy(streamBytes);
            WebsocketClient websocketClient = new WebsocketClient(networkStreamProxy, cb);
            return websocketClient;
        }

        public WebsocketClient CreateWebsocketClient(byte[] streamBytes)
        {
            ChannelBridge cb = new ChannelBridge("");
            MockNetworkStreamProxy networkStreamProxy = new MockNetworkStreamProxy(streamBytes);
            WebsocketClient websocketClient = new WebsocketClient(networkStreamProxy, cb);
            return websocketClient;
        }

        public WebsocketClient CreateWebsocketClient()
        {
            MockNetworkStreamProxy nsp = new MockNetworkStreamProxy("GET /blah");
            WebsocketClient websocketClient = new WebsocketClient(nsp);
            return websocketClient;
        }
    }
}
