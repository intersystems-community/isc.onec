using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NLog;
using isc.onec.bridge;
using System.Text;
using System.Threading;

namespace isc.onec.tcp
{
    public class Processor
    {
        //private static readonly object syncHandle = new object();
        private Dictionary<IPEndPoint, Server> servers = new Dictionary<IPEndPoint, Server>();
        private TcpServer tcpServer;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        public static void Main(string[] args)
        {
            Run(9100, true);
            Console.ReadLine();
        }

        public static void Run(int port, bool keepAlive)
        {
            TcpServer server = new TcpServer(port, keepAlive);
            Processor processor = new Processor(server);
        }

        public Processor(TcpServer server)
        {
            tcpServer = server;
            tcpServer.SocketConnected += new EventHandler<IPEndPointEventArgs>(socketConnected);
            tcpServer.SocketDisconnected += new EventHandler<IPEndPointEventArgs>(socketDisconnected);
            tcpServer.DataReceived += new EventHandler<DataReceivedEventArgs>(dataRecieved);
            tcpServer.SocketError += new EventHandler<IPEndPointEventArgs>(socketError);
        }

        public void socketConnected(object src, IPEndPointEventArgs args)
        {
            logger.Debug("socket "+args.IPEndPoint+" connected.");

            servers[args.IPEndPoint] = new Server();
        }
        public void socketDisconnected(object src, IPEndPointEventArgs args)
        {
            disconnect(args.IPEndPoint);
            logger.Debug("socket "+args.IPEndPoint+" disconnected.");
        }
        public void socketError(object src, IPEndPointEventArgs args)
        {
            disconnect(args.IPEndPoint);
            logger.Error("socket error happens");
        }
        public void dataRecieved(object src, DataReceivedEventArgs args)
        {

            RequestMessage request = (new MessageDecoder(args.Data)).decode();

            //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            //Console.WriteLine(request.ToString());

            if (!serverExists(args.IPEndPoint))
            {
                logger.Error("no Bridge was created previously.");
                return;
            }
            //lock (syncHandle)
            //{
                Server server = servers[args.IPEndPoint];
                string[] reply = server.run(request.command, request.target, request.operand, request.vals, request.types);
            //}

            //Console.WriteLine(reply[0] + ":" + reply[1]);
            //Console.WriteLine((new System.Text.UnicodeEncoding()).GetString(new MessageEncoder(reply).encode()));

            ((TcpServer)src).SendData((new MessageEncoder(reply).encode()), args.IPEndPoint);
        }
        private bool serverExists(IPEndPoint endpoint)
        {
            if (servers.ContainsKey(endpoint)) return true;

            return false;
        }
        
        public void disconnect(IPEndPoint endpoint)
        {
            if (!serverExists(endpoint))
            {
                logger.Error("disconnect():no Bridge was created previously.");
                return;
            }

            Server server = servers[endpoint];
            try
            {
                if (server.isConnected())
                {
                    logger.Debug("sending disconnect() to server in session " + endpoint);
                    server.sendDisconnect();
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("on sendDisconnect()", ex);
            }
            servers.Remove(endpoint);
        }
    }
}
