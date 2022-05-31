using FluentAssertions;
using System.Threading;
using SimpleWebsocketServer;
using Xunit;

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
            client.Handshake();

            client.SendMessage("hello");
            client.SendMessage("/echo hello");
            // TODO:  I need to tell the client to read the hello that the server is meant to echo back 
            //  and assert it's correctness
            Assert.True(false);
            //client.Read();
            client.SendMessage("/auth " + simpleWebsocketServer.adminPassword);
            client.SendMessage("/close");

            // then
            bool threadJoined = t.Join(600);
            threadJoined.Should().BeTrue();
        }

    }
}
