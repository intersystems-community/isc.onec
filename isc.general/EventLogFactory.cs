using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace isc.general {
	public sealed class EventLogFactory {
		private const string DefaultLogName = "Application";

		private static volatile EventLog instance;

		private static object syncRoot = new object();

		private EventLogFactory() {
			// empty
		}

		public static EventLog Instance {
			get {
				IfNotInitialized(() => {
					throw new InvalidOperationException();
				});

				return instance;
			}
		}

		public static void Initialize(ServiceBase service) {
			if (service == null) {
				throw new ArgumentNullException("service");
			}

			IfNotInitialized(() => {
				instance = NewEventLog(service.ServiceName);
			});
		}

		private static void IfNotInitialized(Action action) {
			/*
			 * The CLR resolves DCL-related issues -- see
			 * http://msdn.microsoft.com/en-us/library/ff650316.aspx
			 */
			if (instance == null) {
				lock (syncRoot) {
					if (instance == null) {
						action();
					}
				}
			}
		}

		private static EventLog NewEventLog(string source) {
			const string LogName = DefaultLogName;

			EventLog eventLog = GetEventLog();
			eventLog.Source = source;

			/*
			 * Register a new source for this log if the source is missing.
			 */
			if (!EventLog.SourceExists(source)) {
				EventLog.CreateEventSource(source, LogName);
			}

			return eventLog;
		}

		/// <summary>
		/// Returns the event log instance with the corresponding <em>logName</em>,
		/// creating it if necessary.
		/// </summary>
		/// <param name="logName"></param>
		/// <returns>the event log instance with the corresponding <em>logName</em></returns>
		private static EventLog GetEventLog(string logName) {
			if (EventLog.Exists(logName)) {
				return EventLog.GetEventLogs().Where(eventLog => eventLog.Log == logName).First();
			} else {
				return logName == DefaultLogName ? new EventLog() : new EventLog(logName);
			}
		}

		/// <summary>
		/// Returns the Application event log instance.
		/// </summary>
		/// <returns>the Application event log instance</returns>
		private static EventLog GetEventLog() {
			return GetEventLog(DefaultLogName);
		}
	}
}