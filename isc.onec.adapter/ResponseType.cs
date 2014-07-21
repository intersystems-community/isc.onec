using System;

namespace isc.onec.bridge {
	/// <summary>
	/// Response type. Takes 2 bytes on the wire.
	/// </summary>
	internal enum ResponseType : sbyte {
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
	}

	internal static class ResponseTypeExtensions {
		internal static object GetPrimitiveType(this ResponseType responseType) {
			return Convert.ChangeType(responseType, responseType.GetTypeCode());
		}
	}
}