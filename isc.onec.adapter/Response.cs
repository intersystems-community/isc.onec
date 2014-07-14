using System;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Response</code> are immutable.
	/// </summary>
	public sealed class Response {
		/// <summary>
		/// Response type.
		/// </summary>
		public enum Type {
			/// <summary>
			/// SET/CONNECT/DISCONNECT/FREE
			/// </summary>
			VOID = 1,
			/// <summary>
			/// INVOKE/GET/COUNT
			/// </summary>
			DATA = 2,
			/// <summary>
			/// INVOKE/GET
			/// </summary>
			OBJECT = 3,
			EXCEPTION = 4,
		};

		internal static readonly Response VOID = new Response();

		private readonly Type type;

		private readonly object value;

		private Response() {
			this.type = Type.VOID;
			this.value = "";
		}

		internal Response(Exception e) {
			this.type = Type.EXCEPTION;
			this.value = e.ToStringWithIlOffsets();
		}

		/// <summary>
		/// XXX: Never used.
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

		internal string[] Serialize() {
			return new string[] {
				Convert.ChangeType(this.type, this.type.GetTypeCode()).ToString(),
				this.value == null ? null : this.value.ToString(),
			};
		}
	}
}
