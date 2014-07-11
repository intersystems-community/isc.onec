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
		/// <summary>
		/// Command types sent externally to this server.
		/// </summary>
		public enum Commands {
			/// <summary>
			/// Get an object's property.
			/// </summary>
			GET = 1,
			/// <summary>
			/// Set an object's property.
			/// </summary>
			SET = 2,
			/// <summary>
			/// Invoke an instance method on an object.
			/// </summary>
			INVOKE = 3,
			/// <summary>
			/// Connect a client to an URL.
			/// </summary>
			CONNECT = 4,
			/// <summary>
			/// Disconnect.
			/// </summary>
			DISCONNECT = 5,
			/// <summary>
			/// Remove an object from local cache as well as release the corresponding COM object.
			/// </summary>
			FREE = 6,
			/// <summary>
			/// Return the amount of objects allocated as well as object currently live (cached).
			/// </summary>
			COUNT = 7,
		};

		private V8Service service;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static EventLog eventLog = EventLogFactory.Instance;

		public Server() {
			this.service = new V8Service();
		}

		public string[] Run(int command, string target, string operand, string[] vals, int[] types) {
			var commandType = (Commands) Enum.ToObject(typeof(Commands), command);
			//if target is "." it is context
			try {
				var targetObject = new Request(target == "." ? "" : target);
				return this.DoCommand(commandType, targetObject, operand, vals, types).Serialize();
			} catch (Exception e) {
				var message = e.Source + ":" + this.Client + ":";
				message += commandType.ToString() + ":";
				message += target + ":" + operand + ":" + vals.ToString() + ":" + types.ToString();
				logger.ErrorException(message, e);
				eventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Warning);
				eventLog.WriteEntry(message, EventLogEntryType.Warning);

				this.Disconnect();

				return new Response(e).Serialize();
			}
		}

		public void Disconnect() {
			try {
				if (this.Connected) {
					var journalReport = this.service.getJournalReport();
					if (journalReport != null && journalReport.Length != 0) {
						logger.Debug(journalReport);
						eventLog.WriteEntry(journalReport, EventLogEntryType.Information);
					}

					this.service.Disconnect();
				}
			} finally {
				this.service = null; // XXX: The object can't be reused upon disconnect. 
			}
		}

		public bool Connected {
			get {
				return this.service == null ? false : this.service.Connected;
			}
		}

		public string Client {
			get {
				return this.Connected ? this.service.Client : "null";
			}
		}

		private Response DoCommandIfConnected(Func<Response> f) {
			if (this.Connected) {
				return f();
			}
			return new Response(Response.Type.EXCEPTION, "Not connected");
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
		private Response DoCommand(Commands command,Request obj, string operand, string[] vals, int[] types) {
			switch (command) {
			case Commands.GET:
				return this.DoCommandIfConnected(() => {
					return this.service.get(obj, operand);
				});
			case Commands.SET:
				return this.DoCommandIfConnected(() => {
					Request value = new Request(types[0], vals[0]);
					this.service.Set(obj, operand, value);
					return Response.VOID;
				});
			case Commands.INVOKE:
				return this.DoCommandIfConnected(() => {
					Request[] args = buildRequestList(vals, types);
					return this.service.invoke(obj, operand, args);
				});
			case Commands.CONNECT:
				if (this.service != null) {
					var client = types.Length > 0 ? (string) (new Request(types[0], vals[0])).Value : null;
					this.service.Connect(operand, client);
					return Response.VOID;
				}
				return new Response(Response.Type.EXCEPTION, "Server#service is null");
			case Commands.DISCONNECT:
				this.Disconnect();
				return Response.VOID;
			case Commands.FREE:
				if (this.Connected) {
					this.service.Free(obj);
				}
				return Response.VOID;
			case Commands.COUNT:
				return this.DoCommandIfConnected(() => {
					return this.service.getCounters();
				});
			default:
				/*
				 * Never.
				 */
				throw new Exception("Command not supported");
			}
		}

		private Request[] buildRequestList(string[] values, int[] types)
		{
			if (values.Length != types.Length) throw new Exception("Server: protocol error. Not all values have types.");

			Request[] list = new Request[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				list[i] = new Request(types[i],values[i]);
			}
			return list;
		}
	}
}
