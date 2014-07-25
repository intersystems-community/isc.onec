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
	/// Synchronization policy: thread confined.
	/// Each client connected maintains its own instance.
	/// </summary>
	internal sealed class V8Service {
		private readonly V8Adapter adapter;

		private object context;

		private readonly Repository repository;

		internal string Client {
			get;
			private set;
		}

		private readonly object connectionLock = new object();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		/// <summary>
		/// A shared map of clients to the corresponding V8Service instances.
		/// </summary>
		private static readonly Dictionary<string, V8Service> Clients = new Dictionary<string, V8Service>();

		internal V8Service() {
			Logger.Debug("isc.onec.bridge.V8Service is created");
			this.adapter = new V8Adapter();
			this.repository = new Repository();
		}

		/// <summary>
		/// Connects to the <code>url</code>.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="client">can be <code>null</code></param>
		internal void Connect(string url, string client) {
			Logger.Debug("Client " + client + " connecting to " + url + "...");

			if (this.Connected) {
				throw new InvalidOperationException("Attempt to connect while connected; old client: " + this.Client + "; new client: " + client);
			}

			/*
			 * Make sure we capture client information early,
			 * before any exception is thrown.
			 */
			this.Client = client;
			try {
				if (this.Client != null) {
					lock (Clients) {
						if (Clients.ContainsKey(this.Client)) {
							Logger.Debug("Client with Id = " + this.Client + " was already connected. Disconnecting the other instance...");
							V8Service that = Clients[this.Client];
							Debug.Assert(that != null, "V8Service value at key " + this.Client + " not found");
							Clients.Remove(this.Client);
							try {
								/*
								 * Try to disconnect a stale connection information.
								 */
								that.Disconnect();
								Logger.Debug("Client with Id = " + this.Client + " disconnected. Continuing connection procedure...");
							} catch (Exception e) {
								Logger.DebugException("Client with Id = " + this.Client + " failed to disconnect. Continuing connection procedure anyway...", e);
							}
							Debug.Assert(!Clients.ContainsKey(this.Client), "Failed to remove previous (stale) connection information");
						}
						Clients.Add(this.Client, this);
					}
				}

				this.context = this.adapter.Connect(url);
			} catch {
				Debug.Assert(this.repository.CachedCount == 0 && this.repository.AddedCount == 0,
					"During a connection attempt, the repository should be empty");
				Debug.Assert(this.context == null, "Context should be null");

				if (this.Client != null) {
					lock (Clients) {
						Clients.Remove(this.Client);
					}
				}

				/*
				 * Restore the invariant.
				 */
				this.Client = null;

				Debug.Assert(!this.Connected, "Still pretending to be connected");

				throw;
			}
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

		/// <summary>
		/// Can be called from multiple threads.
		/// </summary>
		internal void Disconnect() {
			Logger.Debug("disconnecting from #" + this.Client + ". Adapter is " + this.adapter);

			/*
			 * Locks *instance* state.
			 */
			lock (this.connectionLock) {
				if (!this.Connected) {
					return;
				}

				DumpClients();

				this.repository.CleanAll(delegate(object rcw) {
					V8Adapter.Free(ref rcw);
				});

				V8Adapter.Free(ref this.context);
				this.adapter.Disconnect();

				if (this.Client != null) {
					/*
					 * Locks *static* (shared) state. Stricter lock, narrower scope.
					 */
					lock (Clients) {
						Clients.Remove(this.Client);
					}
				}

				this.context = null;
				this.Client = null;
			}
		}

		/// <summary>
		/// Only used for logging purposes.
		/// </summary>
		private static void DumpClients() {
			var report = string.Empty;

			lock (Clients) {
				foreach (KeyValuePair<string, V8Service> client in Clients) {
					report += client.Key + "   " + client.Value.adapter.Url + "\n";
				}
			}

			if (report.Length != 0) {
				Logger.Debug(report);
				EventLog.WriteEntry(report, EventLogEntryType.Information);
			}
		}

		internal bool Connected {
			get {
				Debug.Assert(this.adapter.Connected || this.Client == null, "Invariant violated: if disconnected, client can't be non-null");

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
