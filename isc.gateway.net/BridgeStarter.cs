using System;
using System.Diagnostics;
using isc.general;
using isc.onec.tcp.async;
using NLog;

namespace isc.gateway.net {
	public sealed class BridgeStarter : IDisposable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		private readonly int port;

		private readonly bool keepAlive;

		/// <summary>
		/// The default server port number. Must be consistent with the value
		/// of DEFAULT_PORT constant defined in install.cmd/uninstall.cmd batch scripts.
		/// </summary>
		private static readonly int DefaultPort = 9101;

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
			Logger.Debug("BridgeStarter exits");
			this.server.Dispose();
		}

		public void ProcessConnections() {
			try {
				// Instantiate the SocketListener.
				this.server = new TCPAsyncServer(this.port, this.keepAlive);

				var message = "TCP Server started on port " + this.port + ". KeepAlive is " + this.keepAlive;
				Logger.Info(message);
				EventLog.WriteEntry(message);
			} catch (Exception ex) {
				Logger.Error("Unable to start TCP Server: " + ex.Message);
				EventLog.WriteEntry(ex.ToStringWithIlOffsets(), EventLogEntryType.Error);
			}
		}
	}
}
