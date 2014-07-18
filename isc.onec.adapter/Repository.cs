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
		private readonly Dictionary<int, object> cache;

		/// <summary>
		/// XXX: non-synchronized variable access (use either volatile or Interlocked)
		/// </summary>
		private int addedCount;

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		internal Repository() {
			this.cache = new Dictionary<int, object>();
		}

		~Repository() {
			logger.Debug("Repository destructor. Cache has " + cache.Count + " items.");
		}

		internal object Find(int oid) {
			object rcw;

			if (!this.cache.TryGetValue(oid, out rcw)) {
				throw new Exception("Repository: could not Find object #" + oid);
			}
   
			return rcw;
		}

		internal int Add(object rcw) {
			if (this.addedCount == int.MaxValue) {
				throw new InvalidOperationException("Integer overflow");
			}
			int oid = ++this.addedCount;
			this.cache.Add(oid, rcw);
			return oid;
		}

		internal void Remove(int oid) {
			this.cache.Remove(oid);
		}
		
		internal int CachedCount {
			get {
				return this.cache.Count;
			}
		}

		internal int AddedCount {
			get {
				return this.addedCount;
			}
		}
		
		internal void CleanAll(ObjectProcessor processor) {	
			if (processor != null) {
				foreach (KeyValuePair<int, object> pair in this.cache) {
					processor(pair.Value);
				}
			}
			this.cache.Clear();   
		}
	}
}
