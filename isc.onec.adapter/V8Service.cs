using System;
using System.Collections.Generic;
using System.Threading;
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
		private String client;

		internal String Client {
			get {
				return this.client;
			}
		}

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// A shared map of clients to their URL's.
		/// </summary>
		private static readonly Dictionary<string, string> clients = new Dictionary<string, string>();

		internal V8Service() {
			logger.Debug("isc.onec.bridge.V8Service is created");
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
			logger.Debug("connect from session with #" + client);

			if (this.Connected) {
				throw new InvalidOperationException("Attempt to connect while connected; old client: " + this.client + "; new client: " + client);
			}

			/// XXX: attempt to use a state shared among multiple instances w/o proper locking.
			if (client != null) {
				if (clients.ContainsKey(client)) {
					throw new InvalidOperationException("Attempt to create more than one connection to 1C from the same job. Client #" + client);
				} else {
					clients.Add(client, url);
				}
			}

			this.context = this.adapter.Connect(url);
			this.client = client;
		}
	   
		internal void Set(Request target, string property, Request value) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Set() while disconnected");
			}

			object rcw = this.Find(target);
			object argument = this.Marshal(value);
			this.adapter.Set(rcw, property, argument);
		}

		internal Response Get(Request target, string property) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Get() while disconnected");
			}

			object rcw = this.Find(target);
			object returnValue = this.adapter.Get(rcw, property);

			return this.Unmarshal(returnValue);
		}

		internal Response Invoke(Request target, string method, Request[] args) {
			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Invoke() while disconnected");
			}

			object rcw = this.Find(target);
			object[] arguments = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				arguments[i] = this.Marshal(args[i]);
			}
			object returnValue = this.adapter.Invoke(rcw, method, arguments);

			return this.Unmarshal(returnValue);
		}

		internal void Free(Request request) {
			logger.Debug("Freeing object on request " + request.ToString());

			if (!this.Connected) {
				throw new InvalidOperationException("Attempt to call Free() while disconnected");
			}

			object rcw = this.Find(request);
			if (request.Type != RequestType.OBJECT) {
				throw new ArgumentException("V8Service: attempt to Remove non-object");
			} else if (request.Value == null || request.Value.GetType() != typeof(long)) {
				throw new InvalidOperationException("V8Service: expected an OID of type long; actual: " + request.Value);
			}

			this.repository.Remove((long) request.Value);
			this.adapter.Free(ref rcw);
		}

		internal void Disconnect() {
			logger.Debug("disconnecting from #" + this.client + ". Adapter is " + this.adapter);

			if (!this.Connected) {
				return;
			}

			this.repository.CleanAll(delegate(object rcw) {
				this.adapter.Free(ref rcw);
			});

			this.adapter.Free(ref this.context);
			this.adapter.Disconnect();

			if (this.client != null) {
				clients.Remove(this.client);
			}

			this.context = null;
			this.client = null;
		}

		/// <summary>
		/// XXX: Only used for logging, so encapsulate the data returned.
		/// </summary>
		/// <returns></returns>
		internal static string GetJournalReport() {
			var report = "";

			// XXX: clients should be locked during iteration
			foreach (KeyValuePair<string, string> client in clients) {
				report += (client.Key + "   " + client.Value+"\n");
			}

			return report;
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

		private object Marshal(Request value) {
			switch (value.Type) {
			case RequestType.OBJECT:
				return this.Find(value);
			case RequestType.DATA:
			case RequestType.NUMBER:
				return value.Value;
			default:
				/*
				 * CONTEXT
				 */
				return null;
			}
		}

		private Response Unmarshal(object value) {
			if (value is MarshalByRefObject) {
				long oid = this.repository.Add(value);
				return new Response(ResponseType.OBJECT, oid);
			} else if (value != null && value.GetType() == typeof(bool)) {
				return new Response(((bool) value));
			}
			return new Response(ResponseType.DATA, value);
		}

		private object Find(Request request) {
			if (request.Type == RequestType.DATA || request.Type == RequestType.NUMBER) {
				throw new ArgumentException("Expecting either an OBJECT or a CONTEXT request: " + request);
			}
			return request.Type == RequestType.CONTEXT
				? this.context
				: this.repository.Find((long) request.Value);
		}
	}
}
