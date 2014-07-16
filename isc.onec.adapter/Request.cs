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
		/// OBJECT:	OID (long)
		/// CONTEXT:	OID (empty string)
		/// NUMBER:	long
		/// </summary>
		private readonly string value;

		private static EventLog eventLog = EventLogFactory.Instance;

		internal Request(string oid) :
			this(oid.Length == 0 ? RequestType.CONTEXT : RequestType.OBJECT, oid) {
			// empty
		}

		private Request(RequestType type, string value) {
			if (type == RequestType.NUMBER || type == RequestType.OBJECT) {
				try {
					Convert.ToInt64(value);
				} catch (FormatException fe) {
					string message = "Expected a number, received: \"" + value + "\" (length: " + value.Length + ")";
					eventLog.WriteEntry(message, EventLogEntryType.Error);
					throw new ArgumentException(message, fe);
				}
			}

			this.type = type;
			this.value = value;
		}

		internal Request(int typeId, string value) :
			this(ValueOf(typeId), value) {
			// empty
		}

		internal RequestType Type {
			get {
				return this.type;
			}
		}

		internal object Value {
			get {
				return this.type == RequestType.NUMBER || this.type == RequestType.OBJECT
					? Convert.ToInt64(this.value)
					: (object) this.value;
			}
		}

		private static RequestType ValueOf(int number) {
			return (RequestType) Enum.ToObject(typeof(RequestType), number);
		}
	}
}
