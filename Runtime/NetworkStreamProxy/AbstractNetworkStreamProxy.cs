using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebsocketServer
{
    /// <summary>
    /// This class wraps the NetworkStream class allowing us to write
    /// tests and also keep track of helpful debug information in
    /// production.
    /// </summary>
    public abstract class AbstractNetworkStreamProxy : INetworkStream
    {
        private readonly NetworkStream? _networkStream;
        //private MemoryStream? _readLog;
        private readonly MemoryStream? _writeStream;

        public abstract bool DataAvailable { get; }
        public abstract Stream SourceStream { get; }
        public abstract Stream WriteStream { get; }
        public abstract MemoryStream ReadLog { get; set; }
        public abstract int GetBytesAvailable();
        public abstract string GetWritesAsString();
        public abstract byte[] GetWrites();
        public void Read(byte[] buffer, int offset, int count)
        {
            SourceStream.Read(buffer, offset, count);
            ReadLog.Write(buffer, offset, count);
        }
        public int ReadByte()
        {
            int thisByte = SourceStream.ReadByte();
            ReadLog.WriteByte((byte)thisByte);
            return thisByte;
        }
        public void Write(byte[] buffer, int offset, int count)
        {
            WriteStream.Write(buffer, offset, count);
        }
        public void WriteByte(byte value)
        {
            WriteStream.WriteByte(value);
        }
        public void ClearDebugBuffer()
        {
            ReadLog.Close();
            ReadLog = new MemoryStream();
        }

        public string PrintBytesRecieved()
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = ReadLog.ToArray();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString() + " ");
            }
            return sb.ToString();
        }

        public byte[] GetBytesRecieved()
        {
            return ReadLog.ToArray();
        }
    }
}
