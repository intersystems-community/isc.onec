using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace isc.gateway.net
{
    public class ConsoleLogger : ILogger
    {
        public void info(string msg)
        {
            Console.WriteLine(msg);
        }
        public void error(string msg)
        {
            Console.Error.WriteLine(msg);
        }
    }

}
    
