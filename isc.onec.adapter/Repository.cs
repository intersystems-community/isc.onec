using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace isc.onec.bridge
{
    public class Repository
    {
        public delegate void ObjectProcessor(object rcw);
        private Dictionary<long,object> cache;
        private long counter;
        public Repository()
        {
            this.cache = new Dictionary<long, object>();
            this.counter = 0;
        }
        public object find(string oid)
        {
            object rcw;
            long key = Convert.ToInt64(oid);
           
            if(!cache.TryGetValue(key, out rcw)) throw new Exception("Repository: could not find object #"+key);
   
            return rcw;
        }
        public string add(object rcw)
        {
            long key = next();
            cache.Add(key, rcw);
            string oid = Convert.ToString(key);
            return oid;
        }
        public void remove(string oid)
        {
            long key = Convert.ToInt64(oid);
            cache.Remove(key);
        }
        public long countObjectsInCache()
        {
            return cache.Count;
        }

        public long getCurrentCounter()
        {
            return counter;
        }
        public void cleanAll(ObjectProcessor processor)
        {    
            if(processor!= null) {
                List<object> toBeRemoved = new List<object>();
                foreach(KeyValuePair<long,object> pair in cache) {
                    toBeRemoved.Add(pair.Value);
                }
                foreach (object rcw in toBeRemoved)
                {
                    processor(rcw);
                }
                toBeRemoved.Clear();
            }
            cache.Clear();   
        }
        //TODO Make Offset.
        private long next()
        {
            if (counter == Int64.MaxValue) throw new Exception("Repository: maximum number of objects reached");
            counter += 1;
            
            return counter;
        }
    }
}
