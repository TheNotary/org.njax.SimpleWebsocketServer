using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Linq;

namespace SimpleWebsocketServer
{
    public class WebsocketClient : ChannelSubscriber
    {
        INetworkStream _stream;
        WebsocketFrame frame;

        // Bayeux Bridge
        public ChannelBridge channelBridge;

        public bool AdminAuthenticated { get; set; }

        public INetworkStream Stream => _stream;

        internal WebsocketClient(INetworkStream stream) : this(stream, new ChannelBridge("testPass")) { }

        public WebsocketClient(INetworkStream stream, ChannelBridge cb)
        {
            _stream = stream;
            channelBridge = cb;
            AdminAuthenticated = false;
        }

        /// <summary>
        /// This method will consume the next frame from the stream depending on the length of the payload as 
        /// indicated by headerBytes
        /// </summary>
        public WebsocketFrame ConsumeFrameFromStream(byte[] headerBytes)
        {
            frame.fin = (headerBytes[0] & 0b10000000) != 0;
            frame.isMasked = (headerBytes[1] & 0b10000000) != 0;
            frame.opcode = headerBytes[0] & 0b00001111;

            frame.payloadLength = determineMessageLength(headerBytes);
            frame.mask = consumeMask();

            if (frame.payloadLength == 0)
                Console.WriteLine("payloadLength == 0");

            if (frame.isMasked || frame.payloadLength == 0)
            {
                frame.cleartextPayload = decodeMessage().ToArray();

                if (frame.opcode == 0x01) // text message
                {
                    return frame;
                }
                if (frame.opcode == 0x08) // close message
                {
                    frame.closeCode = frame.payloadLength >= 2
                        ? BitConverter.ToUInt16(SubArray(frame.cleartextPayload, 0, 2).Reverse().ToArray())
                        : 0;
                    frame.closeCodeReason = frame.payloadLength > 2
                        ? Encoding.UTF8.GetString(SubArray(frame.cleartextPayload, 2))
                        : "";
                    return frame;
                }
                throw new Exception("Unknown opcode sent from client, crashing connection");
            }
            else
            {
                byte[] clearText = new byte[frame.payloadLength];
                _stream.Read(clearText, 0, clearText.Length);
                return frame;
            }

        }

        public bool ReceiveHttpUpgradeRequest()
        {
            // Get the client's data now that they've at least gotten to the "GE" part of the HTTP upgrade request or the frame header.
            Byte[] headerBytes = new Byte[2];
            _stream.Read(headerBytes, 0, headerBytes.Length);
            String data = Encoding.UTF8.GetString(headerBytes);

            if (data != "GE")  // The handshake always begins with the line "GET " and websocket frames can't begin with G unless an extension was negotiated
                throw new Exception("The first message that came in wasn't a respectable handshake.");

            HttpHandshaker handshaker = new HttpHandshaker(_stream, headerBytes);
            handshaker.ConsumeHttpUpgradeRequestAndCollectWebsocketHeader();
            handshaker.RespondToHandshake();
            Console.WriteLine("Upgraded client to websockets.");
            return true;
        }

        public WebsocketFrame ReceiveMessageFromClient()
        {
            Byte[] headerBytes = new Byte[2];
            _stream.Read(headerBytes, 0, headerBytes.Length);

            WebsocketFrame websocketFrame = ConsumeFrameFromStream(headerBytes);
            return websocketFrame;
        }

        private T[] SubArray<T>(T[] array, int offset, int length)
        {
            T[] result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

        private T[] SubArray<T>(T[] array, int offset)
        {
            int length = array.Length - offset;
            T[] result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

        public ulong determineMessageLength(byte[] headerBytes)
        {
            int msglen = headerBytes[1] & 0b01111111;
            ulong msglen64 = (ulong)msglen;

            if (msglen == 126) // 126 signifies an extended payload size of 16bits
            {
                byte[] balanceBytes = new byte[2];
                _stream.Read(balanceBytes, 0, balanceBytes.Length);

                msglen64 = BitConverter.ToUInt16(balanceBytes.Reverse().ToArray());
            }
            if (msglen == 127)
            {
                byte[] balanceBytes = new byte[8];
                _stream.Read(balanceBytes, 0, balanceBytes.Length);

                msglen64 = BitConverter.ToUInt64(balanceBytes.Reverse().ToArray());
            }

            Console.WriteLine("Payload length was: " + msglen64.ToString());
            return msglen64;
        }

        public MemoryStream decodeMessage()
        {
            MemoryStream decodedStream = new MemoryStream();

            for (ulong i = 0; i < frame.payloadLength; i++)
            {
                byte maskI = (byte)frame.mask[i % 4];
                byte rawByte = (byte)_stream.ReadByte();
                byte decodedByte = (byte)(rawByte ^ maskI);
                decodedStream.WriteByte(decodedByte);
            }
            return decodedStream;
        }

        public byte[] BuildCloseFrame(byte[] closeCodeBytes)
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(0b10001000); // opcode for a finished, closed frame
            output.WriteByte((byte)closeCodeBytes.Length);
            output.Write(closeCodeBytes, 0, closeCodeBytes.Length);
            return output.ToArray();
        }

        /// <summary>
        /// Consumes the next 4 bytes in the stream and returns them as a byte[].  
        /// </summary>
        /// <remarks>
        /// This message assumes the stream cursor is already at the first byte of the mask key
        /// <returns>
        /// The 4 byte mask key that was sent along with the websocket frame under scrutiny.  
        /// If the frame is not masked, will return an empty byte array.
        /// </returns>
        /// </remarks>
        public byte[] consumeMask()
        {
            if (!frame.isMasked)
                return new byte[0];
            byte[] maskingKey = new byte[4];

            _stream.Read(maskingKey, 0, 4);
            return maskingKey;
        }

        public void SendMessage(string msg)
        {
            SendMessage(msg, false);
        }

        public void SendMessage(string msg, bool isMasked)
        {
            byte[] payload = Encoding.UTF8.GetBytes(msg);
            WebsocketFrame sendFrame = new WebsocketFrame();
            sendFrame.fin = true;
            sendFrame.opcode = 0x01;
            sendFrame.isMasked = isMasked;
            sendFrame.payloadLength = (ulong) payload.Length;
            sendFrame.cleartextPayload = payload;

            WebsocketSerializer serializer = new WebsocketSerializer(sendFrame);

            byte[] frameAsBytes = serializer.ToBytes();
            _stream.Write(frameAsBytes, 0, frameAsBytes.Length);
        }

        public override void OnNext(string content)
        {
            Console.WriteLine("Message Received so should be relayed: " + content);
            SendMessage(content);
        }
    }
}
