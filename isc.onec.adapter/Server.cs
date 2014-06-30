using System;
using System.Diagnostics;
using isc.general;
using NLog;

namespace isc.onec.bridge {
	public sealed class Server {
		public enum Commands : int {
			GET = 1,
			SET = 2,
			INVOKE = 3,
			CONNECT = 4,
			DISCONNECT = 5,
			FREE = 6,
			COUNT = 7,
		};

		private V8Service service;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static EventLog eventLog = EventLogFactory.Instance;

		public Server() {
			V8Adapter adapter = new V8Adapter();
			Repository repository = new Repository();

			this.service = new V8Service(adapter, repository);
		}

		public string[] run(int command, string target, string operand, string[] vals, int[] types) {
			Response response;
			var commandType = (Commands) Enum.ToObject(typeof(Commands), command);
			//if target is "." it is context
			try {
				var targetObject = new Request(target == "." ? "" : target);
				response = this.doCommand(commandType, targetObject, operand, vals, types);
			} catch (Exception e) {
				var message = e.Source + ":" + this.Client + ":";
				message += commandType.ToString() + ":";
				message += target + ":" + operand + ":" + vals.ToString() + ":" + types.ToString();
				logger.ErrorException(message, e);
				eventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Warning);
				eventLog.WriteEntry(message, EventLogEntryType.Warning);

				this.Disconnect();

				response = new Response(e);
			}

			string[] reply = this.serialize(response);

			return reply;
		}

		public Response Disconnect() {
			try {
				if (this.Connected) {
					var journalReport = this.service.getJournalReport();
					if (journalReport != null && journalReport.Length != 0) {
						logger.Debug(journalReport);
						eventLog.WriteEntry(journalReport, EventLogEntryType.Information);
					}

					return this.service.disconnect();
				}
				/*
				 * DISCONNECT allows an empty response.
				 */
				return new Response();
			} finally {
				this.service = null;
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

		private Response doCommand(Commands command,Request obj, string operand, string[] vals, int[] types) {
			switch (command) {
			case Commands.GET:
				return this.DoCommandIfConnected(() => {
					return this.service.get(obj, operand);
				});
			case Commands.SET:
				return this.DoCommandIfConnected(() => {
					Request value = new Request(types[0], vals[0]);
					return this.service.set(obj, operand, value);
				});
			case Commands.INVOKE:
				return this.DoCommandIfConnected(() => {
					Request[] args = buildRequestList(vals, types);
					return this.service.invoke(obj, operand, args);
				});
			case Commands.CONNECT:
				if (this.service != null) {
					var client = types.Length > 0 ? (string) (new Request(types[0], vals[0])).Value : null;
					return this.service.connect(operand, client);
				}
				return new Response(Response.Type.EXCEPTION, "Server#service is null");
			case Commands.DISCONNECT:
				return this.Disconnect();
			case Commands.FREE:
				/*
				 * FREE allows an empty response.
				 */
				return this.Connected ? this.service.free(obj) : new Response();
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

		private string[] serialize(Response response) {
			string[] reply = new string[2];
			reply[0] = ((int) response.ResponseType).ToString();
			if (response.Value != null) {
				reply[1] = response.Value.ToString();
			}

			return reply;
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

		public void sendDisconnect()
		{
			string[] result = run((int)Commands.DISCONNECT, "", "", new string[0], new int[0]);
			if (Convert.ToInt32(result[0]) == (int)Response.Type.EXCEPTION)
			{
				throw new Exception("disconnection failed"+result[1]);
			}
		}
	}
}
