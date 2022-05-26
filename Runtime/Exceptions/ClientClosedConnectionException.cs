using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebsocketServer
{
    public class ClientClosedConnectionException : Exception
    {
        public ClientClosedConnectionException(string message) : base(message)
        {
        }
    }
}
