using System;
using System.Text;
using System.Threading;
using Xunit;
using FluentAssertions;
using SimpleWebsocketServer;

namespace SimpleWebsocketServerTest
{
    public class NetworkStreamReaderTest
    {
        [Fact]
        public void ItCanReadALineFromTheStreamAtATime()
        {
            // Given
            string firstLine = "GET / HTTP/1.1";
            string testHttpRequest = $"{firstLine}\r\nHost: server.example.com\r\nUpgrade: websocket\r\nSec-WebSocket-Key: zzz\r\n\r\n";
            MockNetworkStreamProxy networkStreamProxy = new MockNetworkStreamProxy(testHttpRequest);
            NetworkStreamReader nsr = new NetworkStreamReader(networkStreamProxy);

            // When
            string myText = nsr.ReadUntilCarriageReturn();

            // Then
            Assert.Equal(firstLine, myText);
        }

        [Fact]
        public void ItBlocksWhileReadingALineUntilTheStreamHasACarriageReturn()
        {
            // Given
            string firstLine = "GET / HTTP/1.1";
            byte[] eolBytes = Encoding.UTF8.GetBytes("\r\n");

            MockNetworkStreamProxy networkStreamProxy = new MockNetworkStreamProxy(firstLine);

            // When
            Thread t = new Thread(new ParameterizedThreadStart(ReadNetworkStreamInThreadAndEchoToWriteStream));
            t.Start(networkStreamProxy);

            // Then
            Thread.Sleep(50);
            networkStreamProxy.PutBytes(eolBytes);
            t.Join();
            Assert.Equal(firstLine, networkStreamProxy.GetWritesAsString());
        }

        private void ReadNetworkStreamInThreadAndEchoToWriteStream(object? obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            MockNetworkStreamProxy networkStreamProxy = (MockNetworkStreamProxy)obj;
            NetworkStreamReader nsr = new NetworkStreamReader(networkStreamProxy);

            string line = nsr.ReadUntilCarriageReturn();

            byte[] lineBytes = Encoding.UTF8.GetBytes(line);

            networkStreamProxy.Write(lineBytes, 0, lineBytes.Length);
        }


        [Fact]
        public void ItDoesntSetTheStreamPositionToTheVeryEndJustBecauseItReadsUntilCarriageReturn()
        {
            // Given
            string testData = "line 1\r\n";
            testData += "line 2\r\n";
            testData += "line 3\r\n";
            testData += "line 4\r\n";

            MockNetworkStreamProxy networkStreamProxy = new MockNetworkStreamProxy(testData);
            NetworkStreamReader nsr = new NetworkStreamReader(networkStreamProxy);

            // When
            string firstLineRead = nsr.ReadUntilCarriageReturn();

            // Then
            firstLineRead.Should().Be("line 1");
            networkStreamProxy.SourceStream.Position.Should().Be(8);
        }
    }
}
