using Xunit;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using Xunit.Abstractions;
using System.IO.Pipes;
using FluentAssertions;
using SimpleWebsocketServer.Extensions;

namespace SimpleWebsocketServerTest
{
    public class WaysOfTransmittingDataAcrossThreadsTest : BaseTest
    {
        private readonly ITestOutputHelper output;

        public WaysOfTransmittingDataAcrossThreadsTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ItCanUseQueues()
        {
            QueueStream qs = new QueueStream();

            //Thread t = new Thread(new ParameterizedThreadStart(ListenToQueueStreamAndLogData));
            //t.Start(qs);

            // Enqueued Bytes can be Read from the stream
            byte expectedEnqueuedByte = 0x02;
            qs.Enqueue(expectedEnqueuedByte);
            byte actualEnqueuedByte = (byte) qs.ReadByte();
            Assert.Equal(expectedEnqueuedByte, actualEnqueuedByte);

            // ReadByte produces a -1 if the queue is empty
            int nextEnqueuedByte =  qs.ReadByte();
            Assert.Equal(-1, nextEnqueuedByte);

            // Enqueued bytes can be read into a buffer
            byte[] expectedEnqueuedBytes = new byte[] { 0x03, 0x04, 0x05, 0x06 };
            qs.Enqueue(expectedEnqueuedBytes[0]);
            qs.Enqueue(expectedEnqueuedBytes[1]);
            qs.Enqueue(expectedEnqueuedBytes[2]);
            qs.Enqueue(expectedEnqueuedBytes[3]);

            byte[] buffer = new byte[4];
            qs.Read(buffer, 0, buffer.Length);
            Assert.Equal(expectedEnqueuedBytes, buffer);

            // #Write will write to the output stream for future review

            qs.WriteByte(0x07);

            //qs.GetWrites();

            // TODO:  I'm leaving off here because I need to write an actual test first and then implement
            // the missing functionality in either QueueStream or in MockNetworkStreamProxy which may be
            // enough on it's own...


            //qs.WriteByte(0b00000001);
            //qs.WriteByte(0b00000010);
            //qs.WriteByte(0b00000011);
            //qs.WriteByte(0b11111111); // this byte tells the client to close down

            //t.Join();

            Assert.True(true);
        }

        //private void ListenToClientAndLogData(object? svr)
        //{
        //    if (svr == null) throw new ArgumentNullException(nameof(svr));
        //    NetworkStream stream = client.GetStream();

        //    while (!client.Connected) ;
        //    while (client.Connected)
        //    {
        //        while (!stream.DataAvailable) ; // block here till we have data
        //        int myByte = stream.ReadByte();
        //        output.WriteLine(((int)myByte).ToString());
        //        if (myByte == 255) client.Close(); // 6 is our magic disconnect byte
        //    }
        //}


        [Fact]
        public void ItCanUseTcpStreamsToHandleThisDuplexIssue()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8881);
            server.Start();

            Thread t = new Thread(new ParameterizedThreadStart(ListenToClientAndLogData));
            t.Start(server);

            TcpClient client = new TcpClient("127.0.0.1", 8881);

            Socket socket = client.Client;
            socket.Send(new byte[] { 0b00000001 });
            socket.Send(new byte[] { 0b00000010 });
            socket.Send(new byte[] { 0b00000011 });
            socket.Send(new byte[] { 0b11111111 }); // this byte tells the client to close down
            client.Client.Close();

            t.Join();

            Assert.True(true);
        }

        private void ListenToClientAndLogData(object? svr)
        {
            if (svr == null) throw new ArgumentNullException(nameof(svr));
            TcpListener server = (TcpListener) svr;
            TcpClient client = server.AcceptTcpClient();

            Socket socket = client.Client;
            NetworkStream stream = client.GetStream();

            while (!client.Connected) ;
            while (client.Connected)
            {
                while (!stream.DataAvailable) ; // block here till we have data
                int myByte = stream.ReadByte();
                output.WriteLine(((int) myByte).ToString());
                if (myByte == 255) client.Close(); // 6 is our magic disconnect byte
            }
        }


        [Fact]
        public void ItCanUsePipedOutputStreams()
        {
            AnonymousPipeServerStream pipeServer = 
                new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            
            Stream clientPipe = new AnonymousPipeClientStream(PipeDirection.In, pipeServer.GetClientHandleAsString());
            Thread t = new Thread(new ParameterizedThreadStart(ListenToPipe));
            t.Start(clientPipe);

            pipeServer.WriteByte(1);
            pipeServer.WriteByte(2);
            pipeServer.WriteByte(3);
            pipeServer.WriteByte(255);

            t.Join();
        }

        private void ListenToPipe(object? obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            //string pipeHandle = (string) obj;

            Stream pipe = (Stream) obj;

            while (true)
            {
                int myByte = pipe.ReadByte();
                if (myByte == 255) break; // A 255 byte can signal the end of the stream
                output.WriteLine("Data Recieved: " + myByte);
            }
        }

    }
}