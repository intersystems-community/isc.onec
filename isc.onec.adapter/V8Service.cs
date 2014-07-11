using System;
using NLog;
using System.Collections.Generic;
using System.Threading;

namespace isc.onec.bridge {
	public class V8Service {
		private V8Adapter adapter;

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

		private void clearJournal() {
			if (this.client != null) {
				journal.Remove(this.client);
			}
		}

		private void addToJournal(string client, string url) {
			if (client != null) {
				journal.Add(client, url);
			}
		}

		/// <summary>
		/// Connects to the <code>url</code> and returns a <code>void</code> response.
		/// XXX: Should be entered by a single thread, and only once.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="client">can be <code>null</code></param>
		/// <returns>a <code>void</code> response</returns>
		public Response Connect(string url, string client) {
			this.client = client;
			logger.Debug("connect from session with #" + client);
			if (clientExistsInJournal(client))
			{
				Thread.Sleep(1000);
				if (clientExistsInJournal(client))
				{
					throw new InvalidOperationException("Attempt to create more than one connection to 1C from the same job. Client #" + client);
				}
			}
			this.context = this.adapter.connect(url);

			addToJournal(client, url);

			return Response.VOID;
		}



	   
		internal Response set(Request target, string property, Request value)
		{
			object rcw = this.Find(target);
			object argument = this.Marshal(value);
			this.adapter.set(rcw, property, argument);

			return Response.VOID;
		}
		internal Response get(Request target, string property)
		{
			object rcw = Find(target);
			object value = adapter.get(rcw, property);

			return this.Unmarshal(value);
		}

		internal Response invoke(Request target, string method, Request[] args) {
			object rcw = this.Find(target);
			object[] arguments = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				arguments[i] = this.Marshal(args[i]);
			}
			object returnValue = this.adapter.invoke(rcw, method, arguments);

			return this.Unmarshal(returnValue);
		}

		internal Response free(Request request)
		{
			object rcw = this.Find(request);
			this.Remove(request);
			adapter.free(ref rcw);

			logger.Debug("Freeing object on request " + request.ToString());
			
			return Response.VOID;
		}

		internal Response disconnect()
		{
			logger.Debug("disconnecting from #" + this.client +". Adapter is "+adapter.ToString());
			repository.CleanAll(delegate(object rcw) {
				this.adapter.free(ref rcw);
			});

			adapter.free(ref context);
			adapter.disconnect();

			this.adapter = null; // XXX: adapter is only initialized in constructor. This object can't be reused after disconnect.
			clearJournal();

			return Response.VOID;
		}
		internal string getJournalReport() {
			var report = "";

			foreach (KeyValuePair<string, string> pair in journal)
			{
				report += (pair.Key+"   "+pair.Value+"\n");
			}

			return report;
		}

		private bool clientExistsInJournal(string key)
		{
			if (key == null) return false;
			return journal.ContainsKey(key);
		}

		public bool Connected {
			get {
				return this.adapter == null ? false : this.adapter.isConnected;
			}
		}
		public bool isAlive(string url)
		{
			return adapter.isAlive(url);
		}

		public Response getCounters() {
			string reply = repository.CachedCount + "," + repository.AddedCount;
			return new Response(Response.Type.DATA, reply);
		}

		private object Marshal(Request value) {
			switch (value.RequestType) {
			case Request.Type.OBJECT:
				return this.Find(value);
			case Request.Type.DATA:
			case Request.Type.NUMBER:
				return value.Value;
			default:
				/*
				 * CONTEXT
				 */
				return null;
			}
		}

		private Response Unmarshal(object value) {
			if (adapter.isObject(value)) {
				long oid = this.Add(value);
				return new Response(Response.Type.OBJECT, oid);
			} else if (value != null && value.GetType() == typeof(bool)) {
				//bool is serialised to string as True/False
				return new Response(Response.Type.DATA, ((bool) value) ? 1 : 0);
			}
			return new Response(Response.Type.DATA, value);
		}

		private object Find(Request request) {
			if (request.RequestType == Request.Type.DATA || request.RequestType == Request.Type.NUMBER) {
				throw new ArgumentException("Expecting either an OBJECT or a CONTEXT request: " + request);
			}
			return request.RequestType == Request.Type.CONTEXT
				? this.context
				: this.repository.Find((long) request.Value);
		}

		private long Add(object rcw) {
			return this.repository.Add(rcw);
		}

		private void Remove(Request obj) {
			if (obj.RequestType != Request.Type.OBJECT) {
				throw new ArgumentException("V8Service: attempt to Remove non-object");
			} else if (obj.Value == null || obj.Value.GetType() != typeof(long)) {
				throw new InvalidOperationException("V8Service: expected an OID of type long; actual: " + obj.Value);
			}

			this.repository.Remove((long) obj.Value);
		}

		
	}
}
