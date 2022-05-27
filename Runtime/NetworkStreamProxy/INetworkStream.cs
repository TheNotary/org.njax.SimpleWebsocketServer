using System;
using System.IO;

namespace SimpleWebsocketServer
{
    public interface INetworkStream
    {
        bool DataAvailable { get; }
        Stream SourceStream { get; }
        int ReadByte();
        void WriteByte(byte value);
        void Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
        string PrintBytesRecieved();
        void ClearDebugBuffer();
        string GetWritesAsString();
        byte[] GetWrites();

    }
}
