using System;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Response</code> are immutable.
	/// </summary>
	public class Response {
		public enum Type {
			VOID = 1,
			DATA = 2,
			OBJECT = 3,
			EXCEPTION = 4,
		};

		private readonly Type type;

		private readonly object value;

		internal Response() {
			this.type = Type.VOID;
			this.value = "";
		}

		internal Response(Exception e) {
			this.type = Type.EXCEPTION;
			this.value = e.ToStringWithIlOffsets();
		}

		/// <summary>
		/// Never used.
		/// </summary>
		/// <param name="value"></param>
		private Response(bool value) {
			this.type = Type.DATA;
			this.value = value ? "1" : "0";
		}

		internal Response(Type type, object value) {
			if (type == Type.OBJECT && (value == null || value.GetType() != typeof(long))) {
				throw new ArgumentException("Expected value: an OID of type long; actual value: " + value);
			}
			this.type = type;
			this.value = value;
		}

		internal Type ResponseType {
			get {
				return this.type;
			}
		}

		internal object Value {
			get {
				return this.value;
			}
		}
	}
}
