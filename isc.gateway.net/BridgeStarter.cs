using System;
using NLog;
using isc.onec.tcp.async;

namespace isc.gateway.net
{   
   
    public class BridgeStarter : IDisposable
    {
        //TODO normalize object state
    
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int port = DEFAULTPORT;
        private bool keepAlive = true;
        private static int DEFAULTPORT = 9101;

        public TCPAsyncServer server;

        public static void Main(string[] args)
        {
            Console.WriteLine("BridgeStarter");
            new BridgeStarter(args).processConnections();
            Console.ReadLine();
        }

        public BridgeStarter(string[] args)
        {
           
            if (args == null)
            {
                this.port = DEFAULTPORT;
                this.keepAlive = true;

                return;
            }
            if (args.Length == 0)
            {
                this.port = DEFAULTPORT;
                this.keepAlive = true;

                return;
            }
            if (args.Length > 0)
            {
                this.port = Convert.ToInt32(args[0]);
                this.keepAlive = true;
            }
            if (args.Length > 1)
            {
                this.keepAlive = Convert.ToBoolean(args[1]);
            }

        }

       

        public void Dispose()
        {

            logger.Debug("BridgeStarter exits");
        }

        public void processConnections()
        {
            try
            {
                //isc.onec.tcp.Processor.Run(port, keepAlive);
                //isc.onec.tcp.async.TCPAsyncServer.Run(port, keepAlive);

                //instantiate the SocketListener.
                this.server = new TCPAsyncServer(keepAlive, TCPAsyncServer.getSettings(port));

                logger.Info("TCP Server started on port " + port + ". KeepAlive is " + keepAlive);
            }
            catch(Exception ex) {
                logger.Error("Unable to start TCP Server: "+ex.Message);
            }
        }

     
    }
}
