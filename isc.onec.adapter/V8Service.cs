using System;
using System.Collections.Generic;
using System.Diagnostics;
using isc.general;
using NLog;

namespace isc.onec.bridge {
	/// <summary>
	/// Class state invariant:
	/// <ul>
	/// <li>In disconnected mode:<ul>
	/// <li><code>this.adapter.Connected</code> is <code>false</code></li>
	/// <li><code>this.context</code> is <code>null</code></li>
	/// <li><code>this.client</code> is <code>null</code></li>
	/// </ul></li>
	/// <li>In connected mode:<ul>
	/// <li><code>this.adapter.Connected</code> is <code>true</code></li>
	/// <li><code>this.context</code> is non-<code>null</code></li>
	/// <li><code>this.client</code> may be non-<code>null</code></li>
	/// <li><code>this.client</code> may be contained in <code>journal</code> (if non-<code>null</code>)</li>
	/// </ul></li>
	/// </ul>
	/// </summary>
	internal sealed class V8Service {
		private readonly V8Adapter adapter;

		/// <summary>
		/// XXX: shared access w/o synchronization.
		/// </summary>
		private object context;

		private readonly Repository repository;

		/// <summary>
		/// XXX: Shared access.
		/// XXX: This actually looks like a *last connected client*, as the value gets rewritten upon connect.
		/// </summary>
		private string client;

		internal string Client {
			get {
				return this.client;
			}
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		/// <summary>
		/// A shared map of clients to their URL's.
		/// </summary>
		private static readonly Dictionary<string, string> Clients = new Dictionary<string, string>();

		internal V8Service() {
			Logger.Debug("isc.onec.bridge.V8Service is created");
			this.adapter = new V8Adapter();
			this.repository = new Repository();
		}

		/// <summary>
		/// Connects to the <code>url</code>.
		/// XXX: Should be entered by a single thread, and only once.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="client">can be <code>null</code></param>
		internal void Connect(string url, string client) {
			Logger.Debug("connect from session with #" + client);

			if (this.Connected) {
				throw new InvalidOperationException("Attempt to connect while connected; old client: " + this.client + "; new client: " + client);
			}

			// XXX: attempt to use a state shared among multiple instances w/o proper locking.
			if (client != null) {
				if (Clients.ContainsKey(client)) {
					throw new InvalidOperationException("Attempt to create more than one connection to 1C from the same job. Client #" + client);
				} else {
					Clients.Add(client, url);
				}
			}

			this.context = this.adapter.Connect(url);
			this.client = client;
		}
	   
		internal void Set(int oid, string property, Request value) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Set() while disconnected");
			}

			object rcw = this.Find(oid);
			object argument = this.Marshal(value);
			V8Adapter.Set(rcw, property, argument);
		}

		internal Response Get(int oid, string property) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Get() while disconnected");
			}

			object rcw = this.Find(oid);
			object returnValue = V8Adapter.Get(rcw, property);

			return this.Unmarshal(returnValue);
		}

		internal Response Invoke(int oid, string method, Request[] args) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Invoke() while disconnected");
			}

			object rcw = this.Find(oid);
			object[] arguments = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				arguments[i] = this.Marshal(args[i]);
			}
			object returnValue = V8Adapter.Invoke(rcw, method, arguments);

			return this.Unmarshal(returnValue);
		}

		internal void Free(int oid) {
			Logger.Debug("Freeing object with OID " + oid);

			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Free() while disconnected");
			}

			object rcw = this.Find(oid);

			this.repository.Remove(oid);
			V8Adapter.Free(ref rcw);
		}

		internal void Disconnect() {
			Logger.Debug("disconnecting from #" + this.client + ". Adapter is " + this.adapter);

			if (!this.Connected) {
				return;
			}

			DumpClients();

			this.repository.CleanAll(delegate(object rcw) {
				V8Adapter.Free(ref rcw);
			});

			V8Adapter.Free(ref this.context);
			this.adapter.Disconnect();

			if (this.client != null) {
				Clients.Remove(this.client);
			}

			this.context = null;
			this.client = null;
		}

		/// <summary>
		/// Only used for logging purposes.
		/// </summary>
		private static void DumpClients() {
			var report = string.Empty;

			// XXX: clients should be locked during iteration
			foreach (KeyValuePair<string, string> client in Clients) {
				report += client.Key + "   " + client.Value + "\n";
			}

			if (report.Length != 0) {
				Logger.Debug(report);
				EventLog.WriteEntry(report, EventLogEntryType.Information);
			}
		}

		internal bool Connected {
			get {
				return this.adapter.Connected;
			}
		}

		internal Response GetCounters() {
			string value = this.repository.CachedCount + "," + this.repository.AddedCount;
			return new Response(ResponseType.DATA, value);
		}

		/// <summary>
		/// Converts a <code>Request</code> to a COM object or a primitive value.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private object Marshal(Request request) {
			switch (request.Type) {
			case RequestType.OBJECT:
				return this.Find((int) request.Value);
			case RequestType.DATA:
			case RequestType.NUMBER:
				return request.Value;
			default:
				/*
				 * CONTEXT
				 */
				return null;
			}
		}

		/// <summary>
		/// Converts a COM object or a primitive value to a <code>Response</code>.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private Response Unmarshal(object value) {
			if (value is MarshalByRefObject) {
				int oid = this.repository.Add(value);
				return new Response(ResponseType.OBJECT, oid);
			} else if (value != null && value.GetType() == typeof(bool)) {
				return new Response((bool) value);
			}
			return new Response(ResponseType.DATA, value);
		}

		private object Find(int oid) {
			return oid == 0
				? this.context
				: this.repository.Find(oid);
		}
	}
}
