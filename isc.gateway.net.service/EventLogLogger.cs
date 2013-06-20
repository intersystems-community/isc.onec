using System;
using System.Diagnostics;

namespace isc.gateway.net
{
    class EventLogLogger : ILogger
    {
        private EventLog log;

        public EventLogLogger(EventLog log)
        {
            this.log = log;
        }
        public void info(string msg)
        {
            log.WriteEntry(msg, EventLogEntryType.Information);
        }
        public void error(string msg)
        {
            log.WriteEntry(msg, EventLogEntryType.Error);
        }
    }
}
