using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Runtime.InteropServices;
using isc.onec.bridge;
using NLog;
using isc.onec.tcp;

namespace isc.onec.tcp.thread
{
	class ThreadedServer
	{
		private TcpListener tcpListener;
		private Thread listenThread;
		public const Int32 receivePrefixLength = 4;
		public const Int32 sendPrefixLength = 4;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public delegate void MessageHandler(byte[] Packet);
		public event MessageHandler Return_Packet;

		public static void Main(string[] args)
		{
			new ThreadedServer();
		}
		public ThreadedServer()
		{
			this.tcpListener = new TcpListener(IPAddress.Any, 3000);
			this.listenThread = new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
		}
		private void ListenForClients()
		{
			this.tcpListener.Start();

			while (true)
			{
				//blocks until a client has connected to the server
				TcpClient client = this.tcpListener.AcceptTcpClient();
				SetDesiredKeepAlive(client.Client);

				//create a thread to handle communication 
				//with connected client
				Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientConnection));
				clientThread.Start(client);
				
			}
		}
		void parser_Return_Packet(TcpClient client,Server server,byte[] packet)
		{
		   // throw new NotImplementedException();
			//message has successfully been received
			ASCIIEncoding encoder = new ASCIIEncoding();
			//Console.WriteLine(encoder.GetString(Packet, 0, Packet.Length));
			Console.WriteLine("Packet of length " + packet.Length + " is recieved");

			byte[] reply = process(server, packet);
			//byte[] buffer = encoder.GetBytes("Hello Client!"+Packet.Length);

			logger.Debug("Reply:"+encoder.GetString(reply));

			NetworkStream stream = client.GetStream();
			stream.Write(reply, 0, reply.Length);
			stream.Flush();
			
		}
		private void HandleClientConnection(object client)
		{
			logger.Debug("Entering thread.");
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();
			PacketParser parser = new PacketParser();
			parser.Return_Packet += new PacketParser.EventHandler(parser_Return_Packet);

			Server server = new Server();

			byte[] message = new byte[4096];
			int bytesRead;

			while (true)
			{
				bytesRead = 0;

				try
				{
					//blocks until a client sends a message
					bytesRead = clientStream.Read(message, 0, 4096);
					parser.AnalyzeTraffic(ref message, bytesRead,tcpClient,server);
				}
				catch (Exception e)
				{
					logger.DebugException("socket error",e);
					break;
				}

				if (bytesRead == 0)
				{
					logger.Debug("the client has disconnected from the server");
					break;
				}

				
			}
		   
			server = null;
			tcpClient.Close();
			logger.Debug("Exiting thread.");
			logger.Debug("Aborting thread");
			Thread.CurrentThread.Abort();
		}

	   
		private static void SetDesiredKeepAlive(Socket socket)
		{
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			const uint time = 2000;
			const uint interval = 1000;
			SetKeepAlive(socket, true, time, interval);
		}
		static void SetKeepAlive(Socket s, bool on, uint time, uint interval)
		{
			/* the native structure
			struct tcp_keepalive {
			ULONG onoff;
			ULONG keepalivetime;
			ULONG keepaliveinterval;
			};
			*/

			// marshal the equivalent of the native structure into a byte array
			uint dummy = 0;
			var inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
			BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
			BitConverter.GetBytes((uint)time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
			BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

			// call WSAIoctl via IOControl
			int ignore = s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

		}


		private byte[] process(Server server, byte[] data)
		{

			RequestMessage request = (new MessageDecoder(data)).decode();

			//TODO Debug why?
			if (server == null)
			{
				//throw new Exception("OutgoingDataPreparer.process(): no server object");
				logger.Error("OutgoingDataPreparer.process(): no server object.");
				string[] reply = new string[] { ((int)Response.Type.EXCEPTION).ToString(), "OutgoingDataPreparer.process(): no server object" };

				return new MessageEncoder(reply).encode();
			}
			else
			{
				string[] reply = server.run(request.command, request.target, request.operand, request.vals, request.types);

				//logger.Debug("reply:" + reply[0] + "," + reply[1]);

				return new MessageEncoder(reply).encode();
			}
		}
	}
}
