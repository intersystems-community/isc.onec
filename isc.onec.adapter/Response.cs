using System;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Response</code> are immutable.
	/// </summary>
	internal sealed class Response {
		internal static readonly Response VOID = new Response();

		private readonly ResponseType type;

		private readonly object value;

		private Response() {
			this.type = ResponseType.VOID;
			this.value = "";
		}

		internal Response(Exception e) {
			this.type = ResponseType.EXCEPTION;
			this.value = e.ToStringWithIlOffsets();
		}

		/// <summary>
		/// bool is serialised to string as True/False
		/// </summary>
		/// <param name="value"></param>
		internal Response(bool value) {
			this.type = ResponseType.DATA;
			this.value = value ? 1 : 0;
		}

		internal Response(ResponseType type, object value) {
			if (type == ResponseType.OBJECT && (value == null || value.GetType() != typeof(long))) {
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
