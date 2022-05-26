using System.Text;


namespace SimpleWebsocketServer
{
    public class NetworkStreamReader : INetworkStreamReader
    {
        readonly Stream _stream;
        StreamReader _reader;

        public NetworkStreamReader(INetworkStream stream)
        {
            _stream = stream.SourceStream;
            _reader = new StreamReader(_stream);
        }

        public string ReadUntilCarriageReturn()
        {
            StringBuilder sb = new StringBuilder();
            char priorChar;
            char thisChar = (char) '\u0000';
            while (true)
            {
                // There's a bug in peek where it returns -1 if it sees \r as the next character I guess....
                int thisCharI = Read();
                if (thisCharI == -1) // short the loop until there's content and we don't get -1 as our char
                {
                    Thread.Sleep(20);
                    continue;
                }
                priorChar = thisChar;
                thisChar = (char)thisCharI;

                if (thisChar == '\n' && 
                    priorChar == '\r')
                    break; // break when we find "/r/n" in the text

                if (thisChar != '\r')
                    sb.Append(thisChar);
            }

            return sb.ToString();
        }

        private int Read()
        {
            long originalPosition = _stream.CanSeek ? _stream.Position : -1;
            int nextRead = _reader.Read();
            if (nextRead == -1) return -1;

            if (_stream.CanSeek)
                _stream.Position = originalPosition + 1; // This will break on non-ascii my dude...
            return nextRead;
        }
    }
}
