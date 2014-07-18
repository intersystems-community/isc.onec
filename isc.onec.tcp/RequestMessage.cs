using System;
using isc.onec.bridge;

namespace isc.onec.tcp {
	/// <summary>
	/// Used by <code>isc.onec.tcp.async.OutgoingDataPreparer</code>.
	/// </summary>
	public sealed class RequestMessage {
		public static readonly RequestMessage Disconnect = new RequestMessage((int) isc.onec.bridge.Command.DISCONNECT.GetPrimitiveType(),
				"",
				"",
				new int[0],
				new string[0]);

		public int Command {
			get;
			private set;
		}

		public string Target {
			get;
			private set;
		}

		public string Operand {
			get;
			private set;
		}

		/// <summary>
		/// XXX: To be encapsulated.
		/// </summary>
		public readonly int[] types;

		/// <summary>
		/// XXX: To be encapsulated.
		/// </summary>
		public readonly string[] values;

		public int ArgumentCount {
			get {
				return this.types.Length;
			}
		}

		internal RequestMessage(int command,
				string target,
				string operand,
				int[] types,
				string[] values) {
			if (types.Length != values.Length) {
				throw new ArgumentException(types.Length + " != " + values.Length);
			}

			this.Command = command;
			this.Target = target;
			this.Operand = operand;
			this.types = types;
			this.values = values;
		}

		public override string ToString() {
			string header = this.Command + "," + this.Target + "," + this.Operand;
			string data = "values[" + this.ArgumentCount + "]={";
			for (int i = 0; i < this.ArgumentCount; i++) {
				data += this.types[i] + ":" + this.values[i] + ",";
			}
			data += "}";
			return header + data;
		}

		/// <summary>
		/// May be removed if unused.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int GetTypeAt(int index) {
			if (index < 0 || index >= this.ArgumentCount) {
				throw new ArgumentException("Index " + index + " not within the range [0, " + (this.ArgumentCount - 1) + "]");
			}
			return this.types[index];
		}

		/// <summary>
		/// May be removed if unused.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string GetValueAt(int index) {
			if (index < 0 || index >= this.ArgumentCount) {
				throw new ArgumentException("Index " + index + " not within the range [0, " + (this.ArgumentCount - 1) + "]");
			}
			return this.values[index];
		}
	}
}
