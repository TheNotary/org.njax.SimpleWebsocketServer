using System;
using System.IO;
using System.Text;

namespace SimpleWebsocketServerTest
{
    public class FeedableMemoryStream : MemoryStream
    {
        private readonly object putLock = new object();
        long writePosition;
        public FeedableMemoryStream() : base()
        {
            writePosition = 0;
        }

        public FeedableMemoryStream(string initialStreamContents)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(initialStreamContents);
            Write(buffer, 0, buffer.Length);
            Seek(0, SeekOrigin.Begin);
            writePosition = buffer.Length;
        }
        public FeedableMemoryStream(byte[] initialStreamContents)
        {
            Write(initialStreamContents, 0, initialStreamContents.Length);
            Seek(0, SeekOrigin.Begin);
            writePosition = initialStreamContents.Length;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // FIXME:  why is StreamReader#Read() calling THIS of all things?  And especiaqlly with a count of 1024...
            lock (putLock)
            {
                int c = base.Read(buffer, offset, count);
                return c;
            }
        }

        public override int ReadByte()
        {
            lock (putLock)
            {
                return base.ReadByte();
            }
        }
        public void PutByte(int bits)
        {
            lock (putLock)
            {
                unlockedPutByte(bits);
            }
        }

        private void unlockedPutByte(int bits)
        {
            long initialPosition = Position;

            // Make sure the write head hasn't fallen behind due to client writes which...
            // should be restricted actually
            if (writePosition < Position)
                writePosition = Position + 1;

            if (writePosition >= Capacity) // "manually" grow the stream if needed...
            {
                base.WriteByte((byte)bits);
                Position = initialPosition;   // I don't like how race conditiony this feels
                writePosition += 1;
                return;
            }

            byte[] buffer = base.GetBuffer();
            base.SetLength(Length + 1);
            buffer[writePosition] = (byte)bits;
            writePosition++;
        }


        public void PutBytes(byte[] bytes, int v, int length)
        {
            lock (putLock)
            {
                //long currentPosition = this.Position;

                for (int i = 0; i < length; i++)
                {
                    unlockedPutByte(bytes[i]); // Wow I'm lazy...
                }

                //this.Position = currentPosition;
            }
        }

    }
}