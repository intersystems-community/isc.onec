using System;
using System.Diagnostics;
using isc.general;
using NLog;

namespace isc.onec.bridge {
	/// <summary>
	/// A "server" serving a single client. 
	/// Relationship diagram:
	/// Server -> V8Service -> {V8Adapter, Repository, Client}.
	/// </summary>
	public sealed class Server {
		private readonly V8Service service;

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog eventLog = EventLogFactory.Instance;

		public Server() {
			this.service = new V8Service();
		}

		public Response Run(int commandType, string target, string operand, string[] vals, int[] types) {
			var command = (Command) Enum.ToObject(typeof(Command), commandType);
			//if target is "." it is context
			try {
				var obj = new Request(target == "." ? "" : target);
				return this.DoCommand(command, obj, operand, vals, types);
			} catch (Exception e) {
				var message = e.Source + ":" + this.Client + ":";
				message += command.ToString() + ":";
				message += target + ":" + operand + ":" + vals.ToString() + ":" + types.ToString();
				logger.ErrorException(message, e);
				eventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Warning);
				eventLog.WriteEntry(message, EventLogEntryType.Warning);

				this.Disconnect();

				return new Response(e);
			}
		}

		public void Disconnect() {
			if (this.service.Connected) {
				var journalReport = this.service.getJournalReport();
				if (journalReport != null && journalReport.Length != 0) {
					logger.Debug(journalReport);
					eventLog.WriteEntry(journalReport, EventLogEntryType.Information);
				}

				this.service.Disconnect();
			}
		}

		public string Client {
			get {
				return this.service.Connected ? this.service.Client : "null";
			}
		}

		private Response DoCommandIfConnected(Func<Response> f) {
			return this.service.Connected ? f() : Response.NewException("Not connected");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="command"></param>
		/// <param name="obj"></param>
		/// <param name="operand">an URL, or a method or property name.</param>
		/// <param name="vals"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		private Response DoCommand(Command command, Request obj, string operand, string[] vals, int[] types) {
			switch (command) {
			case Command.GET:
				return this.DoCommandIfConnected(() => {
					return this.service.Get(obj, operand);
				});
			case Command.SET:
				return this.DoCommandIfConnected(() => {
					Request value = new Request(types[0], vals[0]);
					this.service.Set(obj, operand, value);
					return Response.VOID;
				});
			case Command.INVOKE:
				return this.DoCommandIfConnected(() => {
					Request[] args = BuildRequestList(vals, types);
					return this.service.Invoke(obj, operand, args);
				});
			case Command.CONNECT:
				var client = types.Length > 0 ? (string) (new Request(types[0], vals[0])).Value : null;
				this.service.Connect(operand, client);
				return Response.VOID;
			case Command.DISCONNECT:
				this.Disconnect();
				return Response.VOID;
			case Command.FREE:
				if (this.service.Connected) {
					this.service.Free(obj);
				}
				return Response.VOID;
			case Command.COUNT:
				return this.DoCommandIfConnected(() => {
					return this.service.getCounters();
				});
			default:
				/*
				 * Never.
				 */
				throw new ArgumentException("Command not supported");
			}
		}

		private static Request[] BuildRequestList(string[] values, int[] types) {
			if (values == null || types == null || values.Length != types.Length) {
				throw new ArgumentException("Server: protocol error. Not all values have types.");
			}

			Request[] requests = new Request[values.Length];
			for (int i = 0; i < values.Length; i++) {
				requests[i] = new Request(types[i], values[i]);
			}
			return requests;
		}
	}
}
