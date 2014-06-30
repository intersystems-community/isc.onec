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

		internal V8Service(V8Adapter adapter, Repository repository) {
			logger.Debug("isc.onec.bridge.V8Service is created");
			this.adapter = adapter;
			this.repository = repository;
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
			object rcw = Find(target);
			object argument;
			argument = marshall(value);
			adapter.set(rcw, property, argument);

			Response response = new Response();

			return response;
		}
		internal Response get(Request target, string property)
		{
			object rcw = Find(target);
			object value = adapter.get(rcw, property);

			return this.unmarshall(value);
		}
		internal Response invoke(Request target, string method, Request[] args)
		{
			object rcw = Find(target);
			object[] arguments = build(args);
			object value = adapter.invoke(rcw, method, arguments);

			return this.unmarshall(value);
		}

		internal Response free(Request request)
		{
			object rcw = Find(request);
			remove(request);
			adapter.free(ref rcw);

			logger.Debug("Freeing object on request " + request.ToString());
			
			Response response = new Response();

			return response;
		}

		internal Response disconnect()
		{
			logger.Debug("disconnecting from #" + this.client +". Adapter is "+adapter.ToString());
			repository.CleanAll(delegate(object rcw) {
				adapter.free(ref rcw);
			});

			adapter.free(ref context);
			adapter.disconnect();

			adapter = null;
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
		public Response getCounters()
		{
			long cacheSize = repository.CountObjectsInCache();
			long counter = repository.CurrentCounter;
			string reply = cacheSize + "," + counter;
			Response response = new Response(Response.Type.DATA, reply);

			return response;
		}

		private object marshall(Request value) {
			switch (value.RequestType) {
			case Request.Type.OBJECT:
				return Find(value);
			case Request.Type.DATA:
			case Request.Type.NUMBER:
				return value.Value;
			default:
				return null;
			}
		}

		private Response unmarshall(object value) {
			if (adapter.isObject(value)) {
				string oid = add(value);
				return new Response(Response.Type.OBJECT, oid);
			} else {
				if (value != null && value.GetType() == typeof(bool)) {
					//bool is serialised to string as True/False
					// XXX: parameter reassignment
					value = (bool) value ? 1 : 0;
				}
				return new Response(Response.Type.DATA, value);
			}
		}
		private object[] build(Request[] args)
		{
			object[] result = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				result[i] = marshall(args[i]);
			}
			return result;
		}

		private object Find(Request obj) {
			return obj.RequestType == Request.Type.CONTEXT
				? this.context
				: this.repository.Find(obj.Value.ToString());
		}

		private string add(object rcw)
		{
			return repository.Add(rcw);
		}
		private void remove(Request obj)
		{
			if (obj.RequestType != Request.Type.OBJECT) throw new Exception("Service: attempt to Remove non-object");

			repository.Remove(obj.Value.ToString());
		}

		
	}
}
