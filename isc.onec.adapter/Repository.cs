using System;
using System.Collections.Generic;
using NLog;

namespace isc.onec.bridge {
	internal sealed class Repository {
		public delegate void ObjectProcessor(object rcw);

		/// <summary>
		/// XXX: Generic.Dictionary is not thread-safe!
		/// Provide locks or replace with Concurrent.Dictionary
		/// (see http://msdn.microsoft.com/en-us/library/dd997305%28v=vs.110%29.aspx).
		/// </summary>
		private readonly Dictionary<long, object> cache;

		/// <summary>
		/// XXX: non-synchronized variable access (use either volatile or Interlocked)
		/// </summary>
		private long counter;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		internal Repository() {
			this.cache = new Dictionary<long, object>();
			this.counter = 0;
		}

		~Repository() {
			logger.Debug("Repository destructor. Cache has " + cache.Count + " items.");
		}

		internal object Find(string oid) {
			object rcw;
			long key = Convert.ToInt64(oid);

			if (!cache.TryGetValue(key, out rcw)) {
				throw new Exception("Repository: could not Find object #" + key);
			}
   
			return rcw;
		}

		internal string Add(object rcw) {
			long key = Next();
			cache.Add(key, rcw);
			string oid = Convert.ToString(key);
			return oid;
		}
	
		internal void Remove(string oid) {
			long key = Convert.ToInt64(oid);
			cache.Remove(key);
		}
		
		internal long CountObjectsInCache() {
			return cache.Count;
		}

		internal long CurrentCounter {
			get {
				return this.counter;
			}
		}
		
		internal void CleanAll(ObjectProcessor processor) {	
			if (processor != null) {
				List<object> toBeRemoved = new List<object>();
				foreach (KeyValuePair<long,object> pair in cache) {
					toBeRemoved.Add(pair.Value);
				}
				foreach (object rcw in toBeRemoved) {
					processor(rcw);
				}
				toBeRemoved.Clear(); // XXX: local variable. WTF?
			}
			this.cache.Clear();   
		}

		private long Next() {
			if (this.counter == Int64.MaxValue) { // XXX: this is a real huge number. WTF?
				throw new Exception("Repository: maximum number of objects reached");
			}

			return ++this.counter;
		}
	}
}
