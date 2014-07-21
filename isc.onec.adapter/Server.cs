using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using isc.general;
using isc.onec.tcp;
using NLog;

namespace isc.onec.bridge {
	/// <summary>
	/// A "server" serving a single client. 
	/// Synchronization policy: thread confined.
	/// Each client connected maintains its own instance.
	/// </summary>
	public sealed class Server {
		private readonly V8Service service;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		public Server() {
			this.service = new V8Service();
			Logger.Debug("A new Server (Id = " + RuntimeHelpers.GetHashCode(this) + ") created.");
		}

		public Response Run(RequestMessage request) {
			Logger.Debug("Server (Id = " + RuntimeHelpers.GetHashCode(this) + "): Run(...) invoked.");
			try {
				switch (request.Command) {
				case Command.GET:
					return this.DoCommandIfConnected(() => {
						return this.service.Get(request.Oid, request.Operand);
					});
				case Command.SET:
					return this.DoCommandIfConnected(() => {
						Request value = new Request(request.GetTypeAt(0), request.GetValueAt(0));
						this.service.Set(request.Oid, request.Operand, value);
						return Response.Void;
					});
				case Command.INVOKE:
					return this.DoCommandIfConnected(() => {
						Request[] args = request.BuildRequestList();
						return this.service.Invoke(request.Oid, request.Operand, args);
					});
				case Command.CONNECT:
					var client = request.ArgumentCount > 0 ? request.GetValueAt(0) : null;
					this.service.Connect(request.Operand, client);
					return Response.Void;
				case Command.DISCONNECT:
					this.Disconnect();
					return Response.Void;
				case Command.FREE:
					if (this.service.Connected) {
						this.service.Free(request.Oid);
					}
					return Response.Void;
				case Command.COUNT:
					return this.DoCommandIfConnected(() => {
						return this.service.GetCounters();
					});
				default:
					/*
					 * Never.
					 */
					throw new ArgumentException("Command not supported");
				}
			} catch (Exception e) {
				var message = e.Source + ":" + this.Client + ":";
				message += request;
				Logger.ErrorException(message, e);
				EventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Warning);
				EventLog.WriteEntry(message, EventLogEntryType.Warning);

				this.Disconnect();

				return new Response(e);
			}
		}

		public void Disconnect() {
			Logger.Debug("Server (Id = " + RuntimeHelpers.GetHashCode(this) + "): disconnect requested.");
			this.service.Disconnect();
		}

		public string Client {
			get {
				return this.service.Connected ? this.service.Client : "null";
			}
		}

		private Response DoCommandIfConnected(Func<Response> f) {
			return this.service.Connected ? f() : Response.NewException("Not connected");
		}
	}
}
