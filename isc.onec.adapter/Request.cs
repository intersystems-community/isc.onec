using System;
using System.Diagnostics;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Request</code> are immutable.
	/// </summary>
	internal sealed class Request {
		internal enum Type {
			DATA = 1,
			OBJECT = 2,
			CONTEXT = 3,
			NUMBER = 4,
		};

		private readonly Type type;

		/// <summary>
		/// DATA:	???
		/// OBJECT:	OID (long)
		/// CONTEXT:	OID (empty string)
		/// NUMBER:	long
		/// </summary>
		private readonly string value;

		private static EventLog eventLog = EventLogFactory.Instance;

		internal Request(string oid) :
			this(oid.Length == 0 ? Type.CONTEXT : Type.OBJECT, oid) {
			// empty
		}

		private Request(Type type, string value) {
			if (type == Type.NUMBER || type == Type.OBJECT) {
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

		internal Type RequestType {
			get {
				return this.type;
			}
		}

		internal object Value {
			get {
				return this.type == Type.NUMBER || this.type == Type.OBJECT
					? Convert.ToInt64(this.value)
					: (object) this.value;
			}
		}

		private static Type ValueOf(int number) {
			return (Type) Enum.ToObject(typeof(Type), number);
		}
	}
}
