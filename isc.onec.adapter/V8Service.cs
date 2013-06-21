using System;
using NLog;
using System.Collections.Generic;
using System.Threading;

namespace isc.onec.bridge
{
    public class V8Service
    {
        private V8Adapter adapter;
        //MarshalByRefObject System.__COMObject
        private object context;
        private Repository repository;
        public String client = null;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static Dictionary<string, string> journal = new Dictionary<string,string>();

        public V8Service(V8Adapter adapter, Repository repository)
        {
            logger.Debug("isc.onec.bridge.V8Service is created");
            this.adapter = adapter;
            this.repository = repository;
        }

        ~V8Service()
        {
            logger.Debug("V8Service destructor is called for #"+client);
            repository.cleanAll(delegate(object rcw) { adapter.free(rcw); });

            adapter.free(context);
            if (isConnected())
            {
                adapter.disconnect();
            }

            clearJournal();
        }

        private void clearJournal()
        {
            if (client != null)
            {
                journal.Remove(client);
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



       
        public Response set(Request target, string property, Request value)
        {
            object rcw = find(target);
            object argument;
            argument = marshall(value);
            adapter.set(rcw, property, argument);

            Response response = new Response();

            return response;
        }
        public Response get(Request target, string property)
        {
            object rcw = find(target);
            object value = adapter.get(rcw, property);

            Response response = unmarshall(value);

            return response;
        }
        public Response invoke(Request target, string method, Request[] args)
        {
            object rcw = find(target);
            object[] arguments = build(args);
            object value = adapter.invoke(rcw, method, arguments);

            Response response = unmarshall(value);

            return response;
        }

        public Response free(Request request)
        {
            object rcw = find(request);
            remove(request);
            adapter.free(rcw);

            logger.Debug("Freeing object on request " + request.ToString());
            
            Response response = new Response();

            return response;
        }

        public Response disconnect()
        {
            logger.Debug("disconnecting from #" + client +". Adapter is "+adapter.ToString());
            repository.cleanAll(delegate(object rcw) { adapter.free(rcw); });

            adapter.free(context);
            adapter.disconnect();

            Response response = new Response();

            return response;
        }
        public string getJournalReport() {
            string report = "\n\r";

            foreach (KeyValuePair<string, string> pair in journal)
            {
                report += (pair.Key+"   "+pair.Value+"\n\r");
            }

            return report;
        }

        private bool clientExistsInJournal(string key)
        {
            if (key == null) return false;
            return journal.ContainsKey(key);
        }

        public bool isConnected()
        {
            if (adapter != null) return adapter.isConnected;
            else return false;
        }
        public bool isAlive(string url)
        {
            return adapter.isAlive(url);
        }
        public Response getCounters()
        {
            long cacheSize = repository.countObjectsInCache();
            long counter = repository.getCurrentCounter();
            string reply = cacheSize + "," + counter;
            Response response = new Response(Response.Type.DATA, reply);

            return response;
        }
        //TODO delegate marshalling to Request class
        //TODO throw exception if no type is found
        private object marshall(Request value)
        {
            switch (value.type)
            {
                case Request.Type.OBJECT: return find(value);//MarshalByRefObject

                case Request.Type.DATA: return value.getValue();

                case Request.Type.NUMBER: return value.getValue();

                default: return null;
            }

          
        }
        //TODO Add NULL type
        private Response unmarshall(object value)
        {
            Response response;
            if (adapter.isObject(value))
            {
                string oid = add(value);
                response = new Response(Response.Type.OBJECT, oid);
            }
            else
            {
                //if(value!=null) Console.WriteLine("V:" + value + ":" + value.GetType());
                if (value != null)
                {
                    //bool is serialised to string as True/False
                    if (typeof(bool) == value.GetType()) value = (bool)value ? 1 : 0; 
                }
                response = new Response(Response.Type.DATA, value);
            }

            return response;
        }
        private object[] build(Request[] args)
        {
            object[] result = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                result[i] = marshall(args[i]);
            }
            return result;
        }
        private object find(Request obj)
        {
            if (obj.type == Request.Type.CONTEXT)
            {
                return this.context;
            }
            return repository.find(obj.value.ToString());
        }
        private string add(object rcw)
        {
            return repository.add(rcw);
        }
        private void remove(Request obj)
        {
            if (obj.type != Request.Type.OBJECT) throw new Exception("Service: attempt to remove non-object");

            repository.remove(obj.value.ToString());
        }

        
    }
}
