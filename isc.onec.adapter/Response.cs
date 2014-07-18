using System;
using isc.general;

namespace isc.onec.bridge {
	/// <summary>
	/// Instances of <code>Response</code> are immutable.
	/// </summary>
	public sealed class Response {
		internal static readonly Response Void = new Response();

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

		/// <summary>
		/// Converts this response into a string array.
		/// </summary>
		/// <returns></returns>
		public string[] Serialize() {
			return new string[] {
				this.type.GetPrimitiveType().ToString(),
				this.value == null ? null : this.value.ToString(),
			};
		}

		/// <summary>
		/// Constructs a new response of type <code>EXCEPTION</code>. 
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static Response NewException(string message) {
			return new Response(ResponseType.EXCEPTION, message);
		}
	}
}
