using System;
using System.Collections.Generic;
using NLog;

namespace isc.onec.bridge {
	internal sealed class Repository {
		internal delegate void ObjectProcessor(object rcw);

		/// <summary>
		/// XXX: Generic.Dictionary is not thread-safe!
		/// Provide locks or replace with Concurrent.Dictionary
		/// (see http://msdn.microsoft.com/en-us/library/dd997305%28v=vs.110%29.aspx).
		/// </summary>
		private readonly Dictionary<long, object> cache;

		/// <summary>
		/// XXX: non-synchronized variable access (use either volatile or Interlocked)
		/// </summary>
		private long addedCount;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		internal Repository() {
			this.cache = new Dictionary<long, object>();
		}

		~Repository() {
			logger.Debug("Repository destructor. Cache has " + cache.Count + " items.");
		}

		internal object Find(long oid) {
			object rcw;

			if (!this.cache.TryGetValue(oid, out rcw)) {
				throw new Exception("Repository: could not Find object #" + oid);
			}
   
			return rcw;
		}

		internal long Add(object rcw) {
			long oid = ++this.addedCount;
			this.cache.Add(oid, rcw);
			return oid;
		}

		internal void Remove(long oid) {
			this.cache.Remove(oid);
		}
		
		internal long CachedCount {
			get {
				return this.cache.Count;
			}
		}

		internal long AddedCount {
			get {
				return this.addedCount;
			}
		}
		
		internal void CleanAll(ObjectProcessor processor) {	
			if (processor != null) {
				foreach (KeyValuePair<long, object> pair in this.cache) {
					processor(pair.Value);
				}
			}
			this.cache.Clear();   
		}
	}
}
