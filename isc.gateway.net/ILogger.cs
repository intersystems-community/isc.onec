using System;


namespace isc.gateway.net
{
    public interface ILogger
    {
        void info(string msg);
        void error(string msg);
    }
}
