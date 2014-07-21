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
			this.value = string.Empty;
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
			if (type == ResponseType.OBJECT && (value == null || value.GetType() != typeof(int))) {
				throw new ArgumentException("Expected value: an OID of type int; actual value: " + value);
			}
			this.type = type;
			this.value = value;
		}

		/// <summary>
		/// Converts this response into a byte array.
		/// </summary>
		/// <returns></returns>
		public byte[] Serialize() {
			sbyte type = (sbyte) this.type.GetPrimitiveType();

			// Type is actually 2 bytes on the wire.
			byte[] tag = BitConverter.GetBytes(type);
			byte[] value = this.value == null
					? new byte[0]
					: System.Text.Encoding.UTF8.GetBytes(this.value.ToString());
			byte[] length = BitConverter.GetBytes(Convert.ToUInt16(value.Length));

			byte[] data = new byte[tag.Length + length.Length + value.Length];
			Buffer.BlockCopy(tag, 0, data, 0, tag.Length);
			Buffer.BlockCopy(length, 0, data, tag.Length, length.Length);
			Buffer.BlockCopy(value, 0, data, tag.Length + length.Length, value.Length);
			return data;
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
