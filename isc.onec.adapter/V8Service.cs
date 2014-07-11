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
		/// XXX: Not set back to null on disconnect; shared access.
		/// </summary>
		private String client = null;

		internal String Client {
			get {
				return this.client;
			}
		}

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static Dictionary<string, string> journal = new Dictionary<string,string>();

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

		private void addToJournal(string client, string operand)
		{
			if (client != null) { journal.Add(client, operand); }
		}

		public Response connect(string url)
		{
		   
			this.context = adapter.connect(url);

			Response response = new Response();

		   

			return response;
		}

		public Response connect(string operand, String client)
		{
			
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
			Response response = connect(operand);

			addToJournal(client, operand);

			return response;
		}



	   
		internal Response set(Request target, string property, Request value)
		{
			object rcw = this.Find(target);
			object argument = this.Marshal(value);
			this.adapter.set(rcw, property, argument);

			return new Response();
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
			
			Response response = new Response();

			return response;
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

			Response response = new Response();

			return response;
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
