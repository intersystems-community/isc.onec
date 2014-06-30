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

		private readonly string value;

		private static EventLog eventLog = EventLogFactory.Instance;

		internal Request(string oid) {
			this.type = oid.Length == 0 ? Type.CONTEXT : Type.OBJECT;
			this.value = oid;
		}

		private Request(Type type, string value) {
			if (type == Type.NUMBER) {
				try {
					Convert.ToInt64(value);
				} catch (FormatException) {
					eventLog.WriteEntry("Expected a number, received: \"" + value + "\" (length: " + value.Length + ")", EventLogEntryType.Error);
					throw;
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
				return this.type == Type.NUMBER
					? Convert.ToInt64(this.value)
					: (object) this.value;
			}
		}

		private static Type ValueOf(int number) {
			return (Type) Enum.ToObject(typeof(Type), number);
		}
	}
}
