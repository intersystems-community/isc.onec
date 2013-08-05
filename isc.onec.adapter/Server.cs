﻿using System;
using System.Diagnostics;
using isc.general;
using NLog;

namespace isc.onec.bridge {
	public class Server {
		public enum Commands:int {
			GET = 1,
			SET = 2,
			INVOKE = 3,
			CONNECT = 4,
			DISCONNET = 5,
			FREE = 6,
			COUNT = 7,
		};

		public V8Service service;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static EventLog eventLog = EventLogFactory.Instance;

		public Server() {
			V8Adapter adapter = new V8Adapter();
			Repository repository = new Repository();

			this.service = new V8Service(adapter, repository);
		}

		//TODO Code smells - should have formalized protocol in commands not something general
		public string[] run(int command, string target, string operand, string[] vals, int[] types) {
			Response response;
			var commandType = Request.numToEnum<Commands>(command);
			//if target is "." it is context
			try {
				var targetObject = new Request(target == "." ? "" : target);
				response = this.doCommand(commandType, targetObject, operand, vals, types);
			} catch (Exception e) {
				var client = this.service == null ? "null" : this.service.client;

				var message = e.Message + " " + e.Source;
				message += client + " :";
				message += commandType.ToString() + ":";
				message += target + ":" + operand + ":" + vals.ToString() + ":" + types.ToString();
				logger.ErrorException(message, e);
				eventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Error);
				eventLog.WriteEntry(message, EventLogEntryType.Error);
				
				if (this.service != null) {
					var journalReport = this.service.getJournalReport();
					if (journalReport != null && journalReport.Length != 0) {
						logger.Debug(journalReport);
						eventLog.WriteEntry(journalReport, EventLogEntryType.Error);
					}

					this.service.disconnect();
					this.service = null;
				}

				response = new Response(e);
			}

			string[] reply = this.serialize(response);

			return reply;
		}

		public bool Connected {
			get {
				return this.service == null ? false : this.service.isConnected();
			}
		}

		private Response doCommand(Commands command,Request obj, string operand, string[] vals, int[] types) {
			switch (command) {
			case Commands.GET:
				return this.service.get(obj, operand);
			case Commands.SET:
				Request value = new Request(types[0], vals[0]);
				return this.service.set(obj, operand, value);
			case Commands.INVOKE:
				Request[] args = buildRequestList(vals, types);
				return service.invoke(obj, operand, args);
			case Commands.CONNECT:
				if (types.Length > 0) {
					Request client = new Request(types[0], vals[0]);
					return service.connect(operand, (String) client.value);
				} else {
					return service.connect(operand, null);
				}
			case Commands.DISCONNET:
				logger.Debug(this.service.getJournalReport());
				Response response = this.service.disconnect(); 
				this.service = null;

				if (this.service != null) {
					this.service = null;
				}

				return response;
			case Commands.FREE:
				return this.service.free(obj);
			case Commands.COUNT:
				return this.service.getCounters();
			default:
				throw new Exception("Command not supported");
			}
		}

		private string[] serialize(Response response) {
			string[] reply = new string[2];
			reply[0] = ((int)response.type).ToString();
			if(response.value!=null) reply[1] = response.value.ToString();
		   
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
			string[] result = run((int)Commands.DISCONNET, "", "", new string[0], new int[0]);
			if (Convert.ToInt32(result[0]) == (int)Response.Type.EXCEPTION)
			{
				throw new Exception("disconnection failed"+result[1]);
			}
		}
	}
 
}
