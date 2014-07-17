using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;

namespace isc.onec.bridge {
	internal sealed class V8Adapter {
		private object connector;

		private static ReaderWriterLock connectorLock = new ReaderWriterLock();

		private bool connected;

		public enum V8Version { V80, V81, V82 };

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public V8Adapter()
		{
			logger.Debug("V8Adapter is created");
		}
		public object get(object target, string name)
		{
			object result;
			try
			{
				result = target.GetType().InvokeMember(name, BindingFlags.GetProperty | BindingFlags.Public, null, target, null);
			}
			catch (TargetInvocationException exception)
			{
				logger.DebugException("get", exception);
				throw exception.InnerException;
			}
			return result;
		}

		public void Set(object target, string property, object value) {
			/*
			 *  target.comObject.GetType().InvokeMember(propertyName, BindingFlags.PutDispProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, target.comObject, new object[] { propertyValue });
			 */
			try
			{
				target.GetType().InvokeMember(property, BindingFlags.SetProperty | BindingFlags.Public, null, target, new object[] { value });
			}
			catch (TargetInvocationException exception)
			{
				logger.DebugException("set", exception);
				throw exception.InnerException;
			}
		}

		internal object invoke(object target, string method, object[] args) {
			try {
				//obj2 = target.comObject.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target.comObject, methodParams, modifiers, null, null);
				// | BindingFlags.Public
				return target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args,null,null,null);
			} catch (TargetInvocationException exception) {
				logger.DebugException("invoke", exception);
				throw exception.InnerException;
			}
		}

		/// <summary>
		/// XXX: Accesses mutable state w/o synchronization
		/// </summary>
		public object Connect(string url) {
			object context;
			try {
				V8Version version = getVersion(url);
				connectorLock.AcquireReaderLock(-1);
				this.connector = this.createConnector(version);
				logger.Debug("New V8.ComConnector is created");
				context = invoke(this.connector, "Connect", new object[] { url });

				this.connected = true;

				logger.Debug("Connection is established");
			} catch (Exception) {
				this.Disconnect();
				throw;
			} finally {
				 connectorLock.ReleaseReaderLock();
			}

			return context;
		}

		/// <summary>
		/// XXX: Accesses mutable state w/o synchronization
		/// </summary>
		public void Disconnect() {
			this.Free(ref this.connector);

			stimulateGC();
			this.connected = false;

			logger.Debug("Disconnection is done.");
			
		}

		public V8Version[] getInstalledVersions()
		{
			V8Version[] values = (V8Version[])Enum.GetValues(typeof(V8Version));
			List<V8Version> list = new List<V8Version>();
			foreach (V8Version version in values)
			{
				try
				{
					this.createConnector(version);
					list.Add(version);
				}
				catch
				{
				}
			}
			return list.ToArray();
		}
		private void stimulateGC()
		{
			GC.Collect();
			GC.Collect();
			//GC.Collect(GC.MaxGeneration);
			GC.WaitForPendingFinalizers();
		}

		private object createConnector(V8Version version) {
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

		public void Free(ref object rcw) {
			if (rcw != null) {
				logger.Debug("Releasing object " + ((MarshalByRefObject)rcw).ToString());
				Marshal.ReleaseComObject(rcw);
				Marshal.FinalReleaseComObject(rcw);

				rcw = null;
			}
		}

		public bool isObject(object val) {
			return (val is MarshalByRefObject);
		}

		private V8Version getVersion(string url) {
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
					throw new NotImplementedException("this version of 1C is not supported");
			}
		}

		/// <summary>
		/// XXX: Accesses mutable state w/o synchronization
		/// </summary>
		internal bool Connected {
			get {
				return this.connected;
			}
		}
	}
}
