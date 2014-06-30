using System;
using System.Collections.Generic;
using NLog;

namespace isc.onec.tcp {
	/// <summary>
	/// Used by <code>isc.onec.tcp.async.OutgoingDataPreparer</code>.
	/// </summary>
	public sealed class MessageDecoder {
		private readonly byte[] data;

		private int current;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public MessageDecoder(byte[] data) {
			this.data = data;
			this.current = 0;
		}

		public  RequestMessage decode() {
			RequestMessage message = new RequestMessage();

			message.command = getCommand(data);
			message.target = getTarget(data);
			message.operand = getOperand(data);

			addValues(message);

			return message;
		}

		private void addValues(RequestMessage message) {
			int num = data[current];
			current += 1;

			message.types = new int[num];
			message.vals = new string[num];

			for (int i = 0; i < num; i++) {
				message.types[i] = this.Type;
				message.vals[i] = this.Value;
			}
		}

		private string Value {
			get {
				//2 bytes
				int length = BitConverter.ToInt16(data, current);
				current += 2;
		   
				string value = (new System.Text.UnicodeEncoding()).GetString(data, current, length*2);
				current += length * 2;

				return value;
			}
		}

		private int Type {
			get {
				int type = data[this.current];
				this.current++;

				return type;
			}
		}

		private string getOperand(byte[] data)
		{
			//2 bytes
			int length = BitConverter.ToInt16(data, 5);
			current += 2;
			current += length * 2;
			return (new System.Text.UnicodeEncoding()).GetString(data,7,length*2);
		}
		//4 byte for int32 
		private string getTarget(byte[] data)
		{
			current += 4;
			int oid = BitConverter.ToInt32(data, 1);
			string target;
			if (oid == 0) target = ".";
			else target = Convert.ToString(oid);
			return target;
		}
		//1 byte for int
		private int getCommand(byte[] data)
		{
			current++;
			return data[0];
		}
	}
}
