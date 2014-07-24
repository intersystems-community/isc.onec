using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using NLog;

namespace isc.onec.bridge {
	/// <summary>
	/// Synchronization policy: thread confined.
	/// Each client connected maintains its own instance.
	/// </summary>
	internal sealed class V8Adapter {
		private object connector;

		private static readonly object ConnectorLock = new object();

		/// <summary>
		/// Set to a non-null value when connected, and back to null when disconnected.
		/// </summary>
		internal string Url {
			get;
			private set;
		}

		private enum V8Version {
			V80,
			V81,
			V82,
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		internal V8Adapter() {
			Logger.Debug("V8Adapter is created");
		}

		/// <summary>
		/// Retrieves the value of a property. 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		internal static object Get(object target, string property) {
			try {
				return target.GetType().InvokeMember(property, BindingFlags.GetProperty | BindingFlags.Public, null, target, null);
			} catch (TargetInvocationException e) {
				Logger.DebugException("Get", e);
				throw e.InnerException;
			}
		}

		/// <summary>
		/// Sets the value of a property.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="property"></param>
		/// <param name="value"></param>
		internal static void Set(object target, string property, object value) {
			try {
////				target.comObject.GetType().InvokeMember(propertyName, BindingFlags.PutDispProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, target.comObject, new object[] { propertyValue });
				target.GetType().InvokeMember(property, BindingFlags.SetProperty | BindingFlags.Public, null, target, new object[] { value });
			} catch (TargetInvocationException e) {
				Logger.DebugException("Set", e);
				throw e.InnerException;
			}
		}

		internal static object Invoke(object target, string method, object[] args) {
			try {
////				obj2 = target.comObject.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target.comObject, methodParams, modifiers, null, null);
				// | BindingFlags.Public
				return target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args, null, null, null);
			} catch (TargetInvocationException exception) {
				Logger.DebugException("invoke", exception);
				throw exception.InnerException;
			}
		}

		internal object Connect(string url) {
			if (url == null || url.Length == 0) {
				throw new ArgumentNullException("url");
			}
			this.Url = url;
			try {
				V8Version version = GetVersion(url);

				lock (ConnectorLock) {
					this.connector = CreateConnector(version);
					Logger.Debug("New V8.ComConnector is created");
					object context = Invoke(this.connector, "Connect", new object[] { url });

					Logger.Debug("Connection is established");
					return context;
				}
			} catch (Exception) {
				this.Disconnect();
				throw;
			}
		}

		internal void Disconnect() {
			this.Url = null;

			Free(ref this.connector);
			Debug.Assert(this.connector == null, "this.connector != null");

			// XXX: Is manual garbage collection really necessary?
			GC.Collect();
			GC.WaitForPendingFinalizers();

			Logger.Debug("Disconnection is done.");
		}

		private static object CreateConnector(V8Version version) {
			string str;
			switch (version) {
			case V8Version.V80:
				str = "V8.ComConnector";
				break;
			case V8Version.V81:
				str = "V81.ComConnector";
				break;
			case V8Version.V82:
				str = "V82.ComConnector";
				break;
			default:
				throw new NotImplementedException();
			}
			Type typeFromProgID = Type.GetTypeFromProgID(str, true);
			return Activator.CreateInstance(typeFromProgID);
		}

		internal static void Free(ref object rcw) {
			if (rcw != null) {
				Logger.Debug("Releasing object " + ((MarshalByRefObject)rcw).ToString());
				Marshal.ReleaseComObject(rcw);
				Marshal.FinalReleaseComObject(rcw);

				rcw = null;
			}
		}

		private static V8Version GetVersion(string url) {
			string version = "V81";
			string[] parameters = url.Split(';');
			for (int i = 0; i < parameters.Length; i++) {
				string[] parameter = parameters[i].Split('=');
				if (parameter[0] == "Version") {
					version = parameter[1].Trim('\"');
				}
			}

			switch (version) {
			case "V80":
				return V8Version.V80;
			case "V81":
				return V8Version.V81;
			case "V82":
				return V8Version.V82;
			default:
				throw new NotImplementedException("1C " + version + " is not supported");
			}
		}

		internal bool Connected {
			get {
				Debug.Assert((this.connector == null) == (this.Url == null),
					"Connector and URL should be consistently null or non-null");

				return this.connector != null;
			}
		}
	}
}
