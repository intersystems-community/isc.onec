using System;
using NLog;
using System.Collections.Generic;
using System.Threading;

namespace isc.onec.bridge {
	public class V8Service {
		private readonly V8Adapter adapter;

		/// <summary>
		/// XXX: shared access w/o synchronization.
		/// </summary>
		private object context;

		private readonly Repository repository;

		/// <summary>
		/// XXX: Not set back to null on disconnect.
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
		private static readonly Dictionary<string, string> journal = new Dictionary<string, string>();

		internal V8Service() {
			logger.Debug("isc.onec.bridge.V8Service is created");
			this.adapter = new V8Adapter();
			this.repository = new Repository();
		}

		private static void RemoveFromJournal(string client) {
			if (client != null) {
				journal.Remove(client);
			}
		}

		private static void AddToJournal(string client, string url) {
			if (client != null) {
				journal.Add(client, url);
			}
		}

		/// <summary>
		/// Connects to the <code>url</code>.
		/// XXX: Should be entered by a single thread, and only once.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="client">can be <code>null</code></param>
		public void Connect(string url, string client) {
			this.client = client;
			logger.Debug("connect from session with #" + client);
			if (ExistsInJournal(client)) {
				Thread.Sleep(1000);
				if (ExistsInJournal(client))
				{
					throw new InvalidOperationException("Attempt to create more than one connection to 1C from the same job. Client #" + client);
				}
			}
			this.context = this.adapter.Connect(url);

			AddToJournal(client, url);
		}



	   
		internal void Set(Request target, string property, Request value) {
			object rcw = this.Find(target);
			object argument = this.Marshal(value);
			this.adapter.Set(rcw, property, argument);
		}

		internal Response Get(Request target, string property)
		{
			object rcw = Find(target);
			object returnValue = adapter.Get(rcw, property);

			return this.Unmarshal(returnValue);
		}

		internal Response Invoke(Request target, string method, Request[] args) {
			object rcw = this.Find(target);
			object[] arguments = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				arguments[i] = this.Marshal(args[i]);
			}
			object returnValue = this.adapter.Invoke(rcw, method, arguments);

			return this.Unmarshal(returnValue);
		}

		internal void Free(Request request) {
			object rcw = this.Find(request);
			this.Remove(request);
			this.adapter.Free(ref rcw);

			logger.Debug("Freeing object on request " + request.ToString());
		}

		internal void Disconnect() {
			logger.Debug("disconnecting from #" + this.client +". Adapter is "+adapter.ToString());
			this.repository.CleanAll(delegate(object rcw) {
				this.adapter.Free(ref rcw);
			});

			this.adapter.Free(ref context);
			this.adapter.Disconnect();

			RemoveFromJournal(this.client);
		}

		internal string getJournalReport() {
			var report = "";

			foreach (KeyValuePair<string, string> pair in journal)
			{
				report += (pair.Key+"   "+pair.Value+"\n");
			}

			return report;
		}

		private static bool ExistsInJournal(string client) {
			return client == null ? false : journal.ContainsKey(client);
		}

		internal bool Connected {
			get {
				return this.adapter.Connected;
			}
		}

		public Response getCounters() {
			string value = repository.CachedCount + "," + repository.AddedCount;
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
				long oid = this.Add(value);
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

		private long Add(object rcw) {
			return this.repository.Add(rcw);
		}

		private void Remove(Request obj) {
			if (obj.Type != RequestType.OBJECT) {
				throw new ArgumentException("V8Service: attempt to Remove non-object");
			} else if (obj.Value == null || obj.Value.GetType() != typeof(long)) {
				throw new InvalidOperationException("V8Service: expected an OID of type long; actual: " + obj.Value);
			}

			this.repository.Remove((long) obj.Value);
		}
	}
}
