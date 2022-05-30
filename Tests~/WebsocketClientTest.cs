using FluentAssertions;
using Xunit;
using SimpleWebsocketServer.Extensions;
using SimpleWebsocketServer;
using System.Text;

namespace SimpleWebsocketServerTest
{
    public class WebsocketClientTest : BaseTest
    {
        string validHttpUpgradeRequest = $"GET / HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nSec-WebSocket-Key: zzz\r\n\r\n";
        byte[] validWebsocketHello = new byte[] { 129, 133, 90, 120, 149, 83, 50, 29, 249, 63, 53 };
        byte[] validClientClose = new byte[] { 136, 130, 104, 40, 78, 91, 107, 193 };
        string validHandshakeResponse = "HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: EJ5xejuUCHQkIKE2QxDTDCDws8Q=\r\n\r\n";

        [Fact]
        public void ItCanConsumeAHelloWebsocketMessageAndOutputAFrame()
        {
            // given
            WebsocketClient websocketClient = CreateWebsocketClient(validWebsocketHello);
            byte[] headerBytes = new byte[2];
            websocketClient.Stream.Read(headerBytes, 0, headerBytes.Length);
            byte[] expectedCleartextBytes = Encoding.UTF8.GetBytes("hello");

            // when
            WebsocketFrame frame = websocketClient.ConsumeFrameFromStream(headerBytes);

            // then
            frame.opcode.Should().Be(0x01);
            frame.fin.Should().BeTrue();
            frame.isMasked.Should().BeTrue();
            frame.mask.Should().Equal(new byte[4] { 90, 120, 149, 83 });
            frame.payloadLength.Should().Be(5);
            frame.cleartextPayload.Should().Equal(expectedCleartextBytes);
        }

        [Fact]
        public void ItCanRecognizeCloseFramesCorrectly()
        {
            // given
            WebsocketClient websocketClient = CreateWebsocketClient(validClientClose);
            byte[] headerBytes = new byte[2];
            websocketClient.Stream.Read(headerBytes, 0, headerBytes.Length);

            // when
            WebsocketFrame frame = websocketClient.ConsumeFrameFromStream(headerBytes);

            // then
            frame.closeCode.Should().Be(1001);
            frame.closeCodeReason.Should().Be("");
        }


        [Fact]
        public void ItCanSendWebsocketFrames()
        {
            // given
            WebsocketClient websocketClient = CreateWebsocketClient(validClientClose);
            byte[] expectedWrittenBytes = new byte[] { 129, 2, 79, 75 };

            // when
            websocketClient.SendMessage("OK");

            // then
            byte[] writtenBytes = websocketClient.Stream.GetWrites();
            writtenBytes.Length.Should().Be(expectedWrittenBytes.Length);
            writtenBytes.Should().Equal(expectedWrittenBytes);
        }

        [Fact]
        public void ItCanUseTheObserverPatternToPumpDataToWebsockets()
        {
            var websocketClient1 = CreateWebsocketClient();
            var websocketClient2 = CreateWebsocketClient();
            string channel = "channel_1";
            string contentToPublish = "HELLO OBSERVERS!";

            ChannelBridge channelBridge = new ChannelBridge("");
            websocketClient1.Subscribe(channelBridge, channel);
            websocketClient2.Subscribe(channelBridge, channel);

            channelBridge.PublishContent(channel, contentToPublish);

            websocketClient1.clientId.Length.Should().Be(36);
            websocketClient1.Stream.GetWritesAsString().Humanize()
                .Should().Be(contentToPublish);
            websocketClient2.Stream.GetWritesAsString().Humanize()
                .Should().Be(contentToPublish);
        }

    }
}
