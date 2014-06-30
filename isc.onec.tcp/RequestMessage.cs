using System;
using System.Collections.Generic;
using System.Text;
using isc.onec.bridge;

namespace isc.onec.tcp {
	/// <summary>
	/// Used by <code>isc.onec.tcp.async.OutgoingDataPreparer</code>.
	/// </summary>
	public sealed class RequestMessage {
		public int command;
		public string target;
		public string operand;
		public string[] vals;
		public int[] types;

		public override string ToString()
		{
			string header = command + "," + target + "," + operand;
			string values = "values["+vals.Length+"]={";
			for (int i = 0; i < vals.Length; i++)
			{
				values += types[i] + ":" + vals[i] + ",";
			}
			values += "}";
			return header + values;
		}

		public static RequestMessage createDisconnectMessage()
		{
			RequestMessage message = new RequestMessage();
			message.command = (int)Server.Commands.DISCONNECT;
			message.target = "";
			message.operand = "";
			message.vals = new string[0];
			message.types = new int[0];

			return message;
		}
	}
}
