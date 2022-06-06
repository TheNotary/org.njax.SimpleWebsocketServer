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
            SimpleWebsocketClient client = new SimpleWebsocketClient(listenAddress, listenPort);
            //WebsocketClient client = new WebsocketClient(listenAddress, listenPort);   // this line is desired final outcome
            // TODO/ FIXME:  When I swap the commenting on the above two lines I get some really
            // odd behavior in this test and it won't pass for nuthin!
            //Assert.True(false);
            client.Connect();

            client.SendMessage("/echo hello");
            WebsocketFrame frame = client.ReceiveMessageFromClient();
            frame.cleartextPayload.Should().Equal(Encoding.UTF8.GetBytes("hello"));

            client.SendMessage("/auth " + simpleWebsocketServer.adminPassword);
            client.SendMessage("/close");

            // then
            bool threadJoined = t.Join(900);
            threadJoined.Should().BeTrue();
        }

    }
}
