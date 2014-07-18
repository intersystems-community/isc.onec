using System;
using isc.onec.bridge;

namespace isc.onec.tcp {
	/// <summary>
	/// Used by <code>isc.onec.tcp.async.OutgoingDataPreparer</code>.
	/// </summary>
	public sealed class RequestMessage {
		public static readonly RequestMessage Disconnect = new RequestMessage(Command.DISCONNECT,
				-1,
				"",
				new int[0],
				new string[0]);

		internal Command Command {
			get;
			private set;
		}

		/// <summary>
		/// An object's OID, or 0 in case this is a context rather than an object,
		/// or -1 in case this request is a disconnect request.
		/// </summary>
		internal int Oid {
			get;
			private set;
		}

		/// <summary>
		/// An URL, or a method or property name.
		/// </summary>
		internal string Operand {
			get;
			private set;
		}

		/// <summary>
		/// XXX: To be encapsulated.
		/// </summary>
		private readonly int[] types;

		/// <summary>
		/// XXX: To be encapsulated.
		/// </summary>
		private readonly string[] values;

		internal int ArgumentCount {
			get {
				return this.types.Length;
			}
		}

		public RequestMessage(byte[] data) {
			int offset = 0;

			// command
			this.Command = (Command) Enum.ToObject(typeof(Command), data[offset++]);

			// OID
			this.Oid = BitConverter.ToInt32(data, offset);
			offset += 4;

			// operand
			this.Operand = ReadString(data, ref offset);

			// types & values
			int argumentCount = data[offset++];

			this.types = new int[argumentCount];
			this.values = new string[argumentCount];
			for (int i = 0; i < argumentCount; i++) {
				this.types[i] = data[offset++];
				this.values[i] = ReadString(data, ref offset);
			}
		}

		private RequestMessage(Command command,
				int oid,
				string operand,
				int[] types,
				string[] values) {
			if (types == null || values == null || types.Length != values.Length) {
				throw new ArgumentException(types.Length + " != " + values.Length);
			}

			this.Command = command;
			this.Oid = oid;
			this.Operand = operand;
			this.types = types;
			this.values = values;
		}

		public override string ToString() {
			string header = this.Command + "," + this.Oid + "," + this.Operand;
			string data = "values[" + this.ArgumentCount + "]={";
			for (int i = 0; i < this.ArgumentCount; i++) {
				data += this.types[i] + ":" + this.values[i] + ",";
			}
			data += "}";
			return header + data;
		}

		internal int GetTypeAt(int index) {
			if (index < 0 || index >= this.ArgumentCount) {
				throw new ArgumentException("Index " + index + " not within the range [0, " + (this.ArgumentCount - 1) + "]");
			}
			return this.types[index];
		}

		internal string GetValueAt(int index) {
			if (index < 0 || index >= this.ArgumentCount) {
				throw new ArgumentException("Index " + index + " not within the range [0, " + (this.ArgumentCount - 1) + "]");
			}
			return this.values[index];
		}

		private static string ReadString(byte[] data, ref int offset) {
			int length = BitConverter.ToInt16(data, offset);
			offset += 2;
			string s = new System.Text.UnicodeEncoding().GetString(data, offset, length * 2);
			offset += length * 2;
			return s;
		}

		internal Request[] BuildRequestList() {
			Request[] requests = new Request[this.ArgumentCount];
			for (int i = 0; i < this.ArgumentCount; i++) {
				requests[i] = new Request(this.types[i], this.values[i]);
			}
			return requests;
		}
	}
}
