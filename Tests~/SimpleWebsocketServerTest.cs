using FluentAssertions;
using System.Threading;
using SimpleWebsocketServer;
using Xunit;
using System.Text;

namespace SimpleWebsocketServerTest
{
    public class SimpleWebsocketServerTest : BaseTest
    {
        [Fact]
        public void ItCanSerializeWebsocketsIntoBytes()
        {
            // when
            string actualPassword = SimpleWebsocketListener.GenerateRandomPassword();

            // then
            actualPassword.Length.Should().Be(10);
        }

        [Fact]
        public void ItCanBeBootedAndClientsCanConnectByIpAndPort()
        {
            string listenAddress = "127.0.0.1";
            int listenPort = 80;

            // given
            SimpleWebsocketListener simpleWebsocketServer = new SimpleWebsocketListener(listenAddress, listenPort);
            var t = new Thread(() =>
            {
                simpleWebsocketServer.Start();
            }); t.Start();

            // when
            //SimpleWebsocketClient client = new SimpleWebsocketClient(listenAddress, listenPort);
            WebsocketClient client = new WebsocketClient(listenAddress, listenPort);   // this line is desired final outcome

            // Theory:  If we wait here for the server to be ready, then things will work or we'll get different error messages at least
            client.Connect();

            client.SendMessage("/echo hello");
            WebsocketFrame frame = client.ReceiveMessageFromClient();
            frame.cleartextPayload.Should().Equal(Encoding.UTF8.GetBytes("hello"));

            client.SendMessage("/auth " + simpleWebsocketServer.adminPassword);
            client.SendMessage("/close");

            // then
            bool threadJoined = t.Join(400);
            threadJoined.Should().BeTrue();
        }

    }
}
