using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebsocketServer
{
    public class ChannelSubscriber : IObserver<string>
    {
        private IDisposable? cancellation;
        public string clientId = Guid.NewGuid().ToString();

        public virtual void Subscribe(ChannelBridge provider, string channel)
        {
            cancellation = provider.Subscribe(this, channel);
        }

        public virtual void Unsubscribe()
        {
            if (cancellation != null)
                cancellation.Dispose();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public virtual void OnNext(string content)
        {
            Console.WriteLine("Message Received so should be relayed: " + content);
        }
    }
}
