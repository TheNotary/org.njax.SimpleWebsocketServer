using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace SimpleWebsocketServerTest
{
    internal class QueueStream : Stream
    {
        private Queue<byte> queue;
        private MemoryStream _outputStream;
        private MemoryStream _inputStream;

        public override bool CanRead => _inputStream.CanRead;

        public override bool CanSeek => _inputStream.CanSeek;

        public override bool CanWrite => _inputStream.CanWrite;

        public override long Length => _inputStream.Length;

        public override long Position { get => _inputStream.Position; set => _inputStream.Position = value; }

        public QueueStream()
        {
            queue = new Queue<byte>();
            _outputStream = new MemoryStream();
            _inputStream = new MemoryStream();
        }

        public void Enqueue(byte value)
        {
            queue.Enqueue(value);
        }

        public override void WriteByte(byte bits)
        {
            _outputStream.WriteByte(bits);
            //queue.Enqueue((byte) bits);
        }

        public override int ReadByte()
        {
            if (queue.Count == 0)
                return -1;
            return (int) queue.Dequeue();
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            int tally = 0;
            for (int i = 0; i < count; i++)
            {
                byte thisByte = new byte();
                if (queue.TryDequeue(out thisByte))
                {
                    tally++;
                    buffer[index + i] = thisByte;
                }
                else
                {
                    break;
                }
            }

            return tally;
        }

        public override void Flush()
        {
            _inputStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inputStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inputStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inputStream.Write(buffer, offset, count);
        }
    }
}