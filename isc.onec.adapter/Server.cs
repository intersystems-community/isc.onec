using System;
using NLog;

namespace isc.onec.bridge
{
    public class Server
    {
        public enum Commands:int { GET=1,SET=2,INVOKE=3,CONNECT=4,DISCONNET=5,FREE=6,COUNT=7 };
        private V8Service service;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Server()
        {
            V8Adapter adapter = new V8Adapter();
            Repository repository = new Repository();

            this.service = new V8Service(adapter, repository);
        }
        //TODO Code smells - should have formalized protocol in commands not something general
        public string[] run(int command,string target,string operand,string[] vals,int[] types) {
            Request targetObject;
            //if target is "." it is context
            if (target != ".") targetObject = new Request(target);
            else targetObject = new Request("");

            Commands commandType = Request.numToEnum<Commands>(command);

            Response response;

            try
            {
                response = doCommand(commandType, targetObject, operand, vals, types);
            }
            catch (Exception e)
            {
                logger.ErrorException(
                    "On cmd:"+commandType.ToString()+",target:"+target
                    +",operand:"+operand+" ,vals:"+vals.ToString()+",types:"+types.ToString()
                    , e);
                response = new Response(e);
            }

            string[] reply = serialize(response);

            return reply;
        }

        public bool isConnected()
        {
            if(service!=null) return service.isConnected();
            return false;
        }

        private Response doCommand(Commands command,Request obj, string operand, string[] vals, int[] types)
        {
            //logger.Debug("cmd:" + command);
            switch (command)
            {
                case Commands.GET: return service.get(obj, operand);
                   
                case Commands.SET:
                    Request value = new Request(types[0], vals[0]);
                    return service.set(obj, operand, value);
                   
                case Commands.INVOKE:
                    Request[] args = buildRequestList(vals, types);
                    return service.invoke(obj, operand, args);
                    
                case Commands.CONNECT: return service.connect(operand);

                case Commands.DISCONNET: Response response = service.disconnect(); this.service = null; return response;
                    
                case Commands.FREE: return service.free(obj);
               
                case Commands.COUNT: return service.getCounters();
                
                default: throw new Exception("Command not supported");
                
            }
        }
        private string[] serialize(Response response) {
            string[] reply = new string[2];
            reply[0] = ((int)response.type).ToString();
            if(response.value!=null) reply[1] = response.value.ToString();
           
            return reply;
        }
        private Request[] buildRequestList(string[] values, int[] types)
        {
            if (values.Length != types.Length) throw new Exception("Server: protocol error. Not all values have types.");

            Request[] list = new Request[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                list[i] = new Request(types[i],values[i]);
            }
            return list;
        }

        public void sendDisconnect()
        {
            string[] result = run((int)Commands.DISCONNET, "", "", new string[0], new int[0]);
            if (Convert.ToInt32(result[0]) == (int)Response.Type.EXCEPTION)
            {
                throw new Exception("disconnection failed"+result[1]);
            }
        }
    }
 
}
