using System;

namespace isc.onec.bridge {
	/// <summary>
	/// Command types sent externally to this server.
	/// </summary>
	public enum Command {
		/// <summary>
		/// Get an object's property.
		/// </summary>
		GET = 1,
		/// <summary>
		/// Set an object's property.
		/// </summary>
		SET = 2,
		/// <summary>
		/// Invoke an instance method on an object.
		/// </summary>
		INVOKE = 3,
		/// <summary>
		/// Connect a client to an URL.
		/// </summary>
		CONNECT = 4,
		/// <summary>
		/// Disconnect.
		/// </summary>
		DISCONNECT = 5,
		/// <summary>
		/// Remove an object from local cache as well as release the corresponding COM object.
		/// </summary>
		FREE = 6,
		/// <summary>
		/// Return the amount of objects allocated as well as object currently live (cached).
		/// </summary>
		COUNT = 7,
	}

	public static class CommandExtensions {
		public static object GetPrimitiveType(this Command command) {
			return Convert.ChangeType(command, command.GetTypeCode());
		}
	}
}