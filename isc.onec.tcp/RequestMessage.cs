using System;
using System.Collections.Generic;
using System.Text;
using isc.onec.bridge;

namespace isc.onec.tcp
{
    public class RequestMessage
    {
        public int command;
        public string target;
        public string operand;
        public string[] vals;
        public int[] types;

        public override string ToString()
        {
            string header = command + "," + target + "," + operand;
            string values = "values["+vals.Length+"]={";
            for (int i = 0; i < vals.Length; i++)
            {
                values += types[i] + ":" + vals[i] + ",";
            }
            values += "}";
            return header + values;
        }

        public static RequestMessage createDisconnectMessage()
        {
            RequestMessage message = new RequestMessage();
            message.command = (int)Server.Commands.DISCONNET;
            message.target = "";
            message.operand = "";
            message.vals = new string[0];
            message.types = new int[0];

            return message;
        }
    }
}
