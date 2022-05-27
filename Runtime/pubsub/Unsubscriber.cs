using System;
using System.Collections.Generic;

namespace SimpleWebsocketServer
{
    internal class Unsubscriber<WebsocketFrame> : IDisposable
    {
        private List<IObserver<WebsocketFrame>> _observers;
        private IObserver<WebsocketFrame> _observer;

        internal Unsubscriber(List<IObserver<WebsocketFrame>> observers, IObserver<WebsocketFrame> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}