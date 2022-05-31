using Microsoft.Extensions.Configuration;
using System;
using System.Threading;

namespace SimpleWebsocketServer
{
    /// <summary>
    /// This application is a basic example of a working websocket server implementation.  
    /// </summary>
    public class WebsocketApp
    {

        static void Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets<WebsocketApp>().Build();

            string adminPassword = configuration["WEBSOCKET_SERVER_ADMIN_PASSWORD"];
            SimpleWebsocketListener simpleWebsocketServer = new SimpleWebsocketListener("0.0.0.0", 80, 2, adminPassword);

            // I guess running this from it's own thread prevents those console bugs where the console wasn't updating until I keypressed it or something...
            var t = new Thread(() => {
                simpleWebsocketServer.Start();
            }); 
            t.Start();
        }

    }

}
