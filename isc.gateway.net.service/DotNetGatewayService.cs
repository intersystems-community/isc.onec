using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using isc.general;
using NLog;

namespace isc.gateway.net {
	internal sealed class DotNetGatewayService : ServiceBase {
		/// <summary>
		/// Must be pure ASCII, no acute allowed.
		/// </summary>
		internal const string ServiceNameTemplate = "Cache One C Bridge";

		private const string DefaultLogName = "Application";

		/// <summary>
		/// Arguments of the Windows service which are passed to BridgeStarter intance.
		/// Currently, the TCP port number and, optionally, the KeepAlive value. 
		/// </summary>
		private String[] args;

		private BackgroundWorker backgroundWorker;

		private BridgeStarter bridgeStarter;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private DotNetGatewayService() {
			this.EventLog.Log = DefaultLogName;
			var commandLineArgs = Environment.GetCommandLineArgs();
			this.ServiceName = commandLineArgs.Length == 1 ? ServiceNameTemplate : ServiceNameTemplate + ' ' + commandLineArgs[1];
			this.EventLog.Source = this.ServiceName;
			/*-
			 * Safety net:
			 *
			 * as long as we're using source name template,
			 * it may be not known to the system,
			 * as service installer uses a different name (which contains the port number).
			 */
			if (!EventLog.SourceExists(this.EventLog.Source)) {
				EventLog.CreateEventSource(this.EventLog.Source, DefaultLogName);
			}

			/*
			 * Okay, now we have service name known and event log source set up.
			 * Let's initialise the (singleton) event logger.
			 */
			EventLogFactory.Initialize(this);

			this.CanHandlePowerEvent = false;
			this.CanHandleSessionChangeEvent = false;
			this.CanPauseAndContinue = false;
			this.CanShutdown = true;
			this.CanStop = true;

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(unhandledExceptionHandler);
		}

		private DotNetGatewayService(String[] args): this() {
			this.args = args;
		}
	 
		public static void Main(String[] args) {
			if (Environment.UserInteractive) {
				Console.WriteLine(typeof(DotNetGatewayService) + " should be run as service");
				return;
			}	  
			ServiceBase.Run(new DotNetGatewayService(args));
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (disposing) {
				this.bridgeStarter.Dispose();
			}
		}

		protected override void OnStart(string[] args) {
			//It has really no sense on normal server, but RG has troubles with virtual machine start time
			this.RequestAdditionalTime(120000);

			if (args.Length != 0) {
				this.args = args;
			}

			/*
			 * Either use the updated args passed to the OnStart(...) method,
			 * or fall back to those supplied during service creation.
			 */
			this.bridgeStarter = new BridgeStarter(this.args);

			this.backgroundWorker = new BackgroundWorker();

			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

			this.backgroundWorker.RunWorkerAsync(this.bridgeStarter);
		}

		private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			if (e.Error != null) {
				this.EventLog.WriteEntry(e.Error.ToStringWithIlOffsets(), EventLogEntryType.Error);
				logger.ErrorException("bw_RunWorkerCompleted: Exception happened, stopping ... ", e.Error);
				this.ExitCode = 1;
				this.Stop();
			}
		}

		private static void bw_DoWork(object sender, DoWorkEventArgs e) {
			((BridgeStarter) (e.Argument)).ProcessConnections();
		}

		private void unhandledExceptionHandler(object sender, UnhandledExceptionEventArgs ue) {
			Exception e = (Exception)ue.ExceptionObject;
			this.EventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Error);
			logger.ErrorException("Stopping. Unhandled exception happened. ",e);
			this.ExitCode = 1;
			this.Stop();
		}

		protected override void OnStop() {
			//Of course it would be better to close all sockets immedie
			this.RequestAdditionalTime(120000);
			logger.Info("Cache Bridge Service is stopping.");
			this.bridgeStarter.Dispose();
			this.backgroundWorker.CancelAsync();
			this.backgroundWorker.Dispose();
		}

		protected override void OnShutdown() {
			logger.Info("Cache Bridge Service was shutdowned.");
			this.EventLog.WriteEntry("Cache Bridge Service was shutdowned.", EventLogEntryType.Information);
			base.OnShutdown();
		}

		protected override void OnCustomCommand(int command) {
			logger.Info("Cache Bridge Service was custom commanded."+command);
			this.EventLog.WriteEntry("Cache Bridge Service custom commanded."+command, EventLogEntryType.Information);
			base.OnCustomCommand(command);
		}

		protected override void OnContinue() {
			logger.Info("Cache Bridge Service was continued.");
			this.EventLog.WriteEntry("Cache Bridge Service was continued.", EventLogEntryType.Information);
			base.OnContinue();
		}

		protected override void OnPause() {
			logger.Info("Cache Bridge Service was paused.");
			this.EventLog.WriteEntry("Cache Bridge Service was paused", EventLogEntryType.Information);
			base.OnPause();
		}
	}
}
