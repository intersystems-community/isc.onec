using System;

namespace isc.onec.bridge {
	internal enum RequestType {
		DATA = 1,
		OBJECT = 2,
		CONTEXT = 3,
		NUMBER = 4,
	}

	internal static class RequestTypeExtensions {
		internal static object GetPrimitiveType(this RequestType requestType) {
			return Convert.ChangeType(requestType, requestType.GetTypeCode());
		}
	}
}