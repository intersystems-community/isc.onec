using System;
using System.Diagnostics;
using isc.general;

namespace isc.onec.bridge
{
	public class Request
	{
		public enum Type { DATA=1,OBJECT=2,CONTEXT=3,NUMBER=4 };

		private Type type;

		private string value;

		private static EventLog eventLog = EventLogFactory.Instance;

		public Request(string oid)
		{
			if (oid == "")
			{
				this.type = Type.CONTEXT;
			}
			else
			{
				this.type = Type.OBJECT;
			}
			this.value=oid;
		}

		public Request(Type type, string value) {
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

		public Request(int typeId, string value) :
			this(Request.numToEnum<Type>(typeId), value) {
			// empty
		}

		public Type RequestType {
			get {
				return this.type;
			}
		}

		public object Value {
			get {
				return this.type == Type.NUMBER
					? Convert.ToInt64(this.value)
					: (object) this.value;
			}
		}

		public static T numToEnum<T>(int number)
		{
			return (T)Enum.ToObject(typeof(T), number);
		}
	}
}
