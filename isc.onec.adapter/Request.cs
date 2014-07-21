using System;
using System.Diagnostics;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Request</code> are immutable.
	/// </summary>
	internal sealed class Request {
		private readonly RequestType type;

		/// <summary>
		/// DATA:	???
		/// OBJECT:	OID (int)
		/// CONTEXT:	OID (empty string)
		/// NUMBER:	long
		/// </summary>
		private readonly string value;

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		internal Request(RequestType type, string value) {
			if (type == RequestType.OBJECT) {
				try {
					Convert.ToInt32(value);
				} catch (FormatException fe) {
					string message = "Expected a 32-bit signed integer, received: \"" + value + "\" (length: " + value.Length + ")";
					EventLog.WriteEntry(message, EventLogEntryType.Error);
					throw new ArgumentException(message, fe);
				}
			} else if (type == RequestType.NUMBER) {
				try {
					Convert.ToInt64(value);
				} catch (FormatException fe) {
					string message = "Expected a 64-bit signed integer, received: \"" + value + "\" (length: " + value.Length + ")";
					EventLog.WriteEntry(message, EventLogEntryType.Error);
					throw new ArgumentException(message, fe);
				}
			}

			this.type = type;
			this.value = value;
		}

		internal RequestType Type {
			get {
				return this.type;
			}
		}

		internal object Value {
			get {
				switch (this.type) {
				case RequestType.OBJECT:
					return Convert.ToInt32(this.value);
				case RequestType.NUMBER:
					return Convert.ToInt64(this.value);
				default:
					return this.value;
				}
			}
		}
	}
}
