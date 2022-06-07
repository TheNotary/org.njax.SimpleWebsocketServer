using System;
using System.IO;
using System.Text;
using SimpleWebsocketServer;

namespace SimpleWebsocketServerTest
{
    public class MockNetworkStreamProxy : AbstractNetworkStreamProxy
    {
        private readonly FeedableMemoryStream _networkStream;
        private readonly MemoryStream _writeStream;
        
        public MockNetworkStreamProxy(string testString)
        {
            _networkStream = new FeedableMemoryStream(testString);
            _writeStream = new MemoryStream();
        }
        public MockNetworkStreamProxy(byte[] testBytes)
        {
            _networkStream = new FeedableMemoryStream(testBytes);
            _writeStream = new MemoryStream();
        }

        public override bool DataAvailable => IsDataAvailable();
        public override Stream SourceStream => (Stream)_networkStream;
        public override Stream WriteStream => (Stream)_writeStream;
        public override MemoryStream ReadLog { get; set; } = new MemoryStream();

        public void PutByte(byte value)
        {
            _networkStream.PutByte(value);
        }

        public void PutBytes(byte[] bytes)
        {
            _networkStream.PutBytes(bytes, 0, bytes.Length);
        }

        /* This message return, as a string, all the characters that were written to the _writeStream 
         * which simulates what this client would have written to the tcp stream.
         * */
        public override string GetWritesAsString()
        {
            return Encoding.UTF8.GetString(_writeStream.ToArray());
        }
        public override byte[] GetWrites()
        {
            return _writeStream.ToArray();
        }

        public bool IsDataAvailable()
        {
            return SourceStream.Position < SourceStream.Length;
        }

        public override int GetBytesAvailable()
        {
            if (SourceStream.Position < SourceStream.Length)
                    return Convert.ToInt32(SourceStream.Length - SourceStream.Position);
            if (IsDataAvailable())
                return Convert.ToInt32(SourceStream.Length - SourceStream.Position);
            return 0;
        }
    }
}
