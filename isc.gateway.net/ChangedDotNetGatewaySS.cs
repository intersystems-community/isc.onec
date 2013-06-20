using System;
using System.Net;
using System.Net.Sockets;

namespace isc.gateway.net
{
    public class ChangedDotNetGatewaySS:IDisposable
    {
        //TODO normalize object state
        private string[] args;
        private ILogger logger = new ConsoleLogger();
        private TcpListener listener = null;
        public ChangedDotNetGatewaySS(string[] args)
        {
            this.args = args;

        }

        public void addLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public void Dispose()
        {
            if (listener != null)
            {
                listener.Stop();
            }

        }

        public void processConnections()
        {
            string log = "";
            string ipString = "";
            if (args.Length == 0)
            {
                string msg = "Syntax Error: DotNetGatewaySS [Port] [Host] [Logfile] [ClassLevel]\n\tPort = number must be supplied; \n\tHost = Optional Host Name or IP Address to listen on ... \n\t\t(\"\" and default listens on all IP addresses); \n\tLogfile = Optional; \n\tClassLevel = Optional;\n";
                logger.error(msg);
                throw new ArgumentException(msg);
            }
            else
            {
                int port = int.Parse(args[0]);
                if (args.Length > 1)
                {
                    ipString = args[1];
                    if (args.Length > 2)
                    {
                        log = args[2];
                        if (args.Length == 4)
                        {
                            int.Parse(args[3]);
                        }
                    }
                }

                try
                {
                    if (ipString.Length == 0)
                    {
                        ipString = "0.0.0.0";
                    }
                    IPAddress address = null;
                    IPAddress.TryParse(ipString, out address);
                    if (address == null)
                    {
                        address = Dns.GetHostEntry(ipString).AddressList[0];
                    }
                    IPEndPoint localEP = new IPEndPoint(address, port);
                    listener = new TcpListener(localEP);
                    listener.Start(10);
                    logger.info("\nListening on IP:Port - " + listener.LocalEndpoint + "\n");
                    while (true)
                    {
                        TcpClient sock = listener.AcceptTcpClient();
                        sock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        new InterSystems.Data.CacheClient.Gateway.Gateway(sock, port, log).Start();
                    }
                }
                catch (Exception exception)
                {
                    string msg = "[DotNet Gateway] Communication link failure!\n" + exception.ToString();
                    logger.error(msg);
                    listener.Stop();
                    throw new ApplicationException(msg);

                }
            }
        }
    }
}
