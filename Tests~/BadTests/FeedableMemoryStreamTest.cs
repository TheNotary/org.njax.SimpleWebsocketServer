using System;
using System.Text;
using System.Threading;
using Xunit;
using FluentAssertions;
using SimpleWebsocketServerTest;
using SimpleWebsocketServer;

namespace SimpleWebsocketServerTest
{
    public class FeedableMemoryStreamTest
    {
        [Fact]
        public void ItCanUseFeedableMemoryStreams()
        {
            // Given
            FeedableMemoryStream fms = new FeedableMemoryStream();
            StringBuilder sb = new StringBuilder();

            // When
            var t = new Thread(() =>
            {
                while (true)
                {
                    if (fms.Position < fms.Length)
                    {
                        int myByte = fms.ReadByte();
                        if (myByte == 255) break; // A 255 byte can signal the end of the stream
                        sb.Append(myByte.ToString());
                    }
                    Thread.Sleep(5);
                }
            }); t.Start();

            // And
            fms.PutByte(1);
            fms.PutByte(2);
            fms.PutByte(3);
            fms.PutByte(255);

            // Then
            t.Join();
            Assert.Equal("123", sb.ToString());
        }

        [Fact]
        public void ItDoesntOverwriteTheInitialStreamWhenPutBytesIsUsed()
        {
            // Given
            FeedableMemoryStream fms = new FeedableMemoryStream("Hello");

            // When
            fms.PutByte(1);
            fms.PutByte(2);
            fms.PutByte(3);

            // Then
            Byte[] actualBytes = new byte[8];
            fms.Read(actualBytes, 0, actualBytes.Length);

            new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x01, 0x02, 0x03 }.Should().Equal(actualBytes);
        }

        [Fact]
        public void PutBytesIsThreadsafe()
        {
            //given
            MockNetworkStreamProxy stream = new MockNetworkStreamProxy("GET\r\n");
            NetworkStreamReader networkStreamReader = new NetworkStreamReader((INetworkStream)stream);

            string firstLine = "";
            string secondLine = "";
            // when
            var t = new Thread(() => {
                firstLine = networkStreamReader.ReadUntilCarriageReturn();
                secondLine = networkStreamReader.ReadUntilCarriageReturn();
            }); t.Start();

            stream.PutBytes(Encoding.UTF8.GetBytes("ABC\r\n"));

            t.Join();



            // then
            firstLine.Should().Be("GET");
            secondLine.Should().Be("ABC");
            //Assert.Equal(validHandshakeResponse, networkStreamProxy.GetWritesAsString());
        }

    }
}
