using System;

namespace isc.onec.bridge {
	/// <summary>
	/// Response type.
	/// </summary>
	public enum ResponseType {
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

	public static class ResponseTypeExtensions {
		public static object GetPrimitiveType(this ResponseType responseType) {
			return Convert.ChangeType(responseType, responseType.GetTypeCode());
		}
	}
}