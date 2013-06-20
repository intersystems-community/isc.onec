using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace isc.gateway.net
{
    public interface ILogger
    {
        void info(string msg);
        void error(string msg);
    }
}
