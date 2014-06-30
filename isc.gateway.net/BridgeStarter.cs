using System;
using System.Diagnostics;
using isc.general;
using isc.onec.tcp.async;
using NLog;

namespace isc.gateway.net {
	public sealed class BridgeStarter : IDisposable {
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static EventLog eventLog = EventLogFactory.Instance;

		private readonly int port;

		private readonly bool keepAlive;

		/// <summary>
		/// The default server port number. Must be consistent with the value
		/// of DEFAULT_PORT constant defined in install.cmd/uninstall.cmd batch scripts.
		/// </summary>
		private static int DefaultPort = 9101;

		private TCPAsyncServer server;

		public BridgeStarter(string[] args) {
			if (args == null || args.Length == 0) {
				this.port = DefaultPort;
				this.keepAlive = true;
			} else {
				this.port = Convert.ToInt32(args[0]);
				this.keepAlive = args.Length > 1 ? this.keepAlive = Convert.ToBoolean(args[1]) : true;
			}
		}

		/// <summary>
		/// <see cref = "System.IDisposable.Dispose()"/>
		/// </summary>
		public void Dispose() {
			logger.Debug("BridgeStarter exits");
			this.server.Dispose();
		}

		public void ProcessConnections() {
			try {
				//instantiate the SocketListener.
				this.server = new TCPAsyncServer(this.port, keepAlive);

				var message = "TCP Server started on port " + port + ". KeepAlive is " + keepAlive;
				logger.Info(message);
				eventLog.WriteEntry(message);
			} catch(Exception ex) {
				logger.Error("Unable to start TCP Server: "+ex.Message);
				eventLog.WriteEntry(ex.ToStringWithIlOffsets(), EventLogEntryType.Error);
			}
		}
	}
}
