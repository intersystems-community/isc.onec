using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.ComponentModel;
using NLog;

namespace isc.gateway.net
{
    public class DotNetGatewayService : ServiceBase
    {

        //public const string serviceName = "Caché One C Bridge";
        public const string serviceName = "Cache One C Bridge";
        private String[] args;
        private BackgroundWorker bw;
        //private ChangedDotNetGatewaySS worker;
        private BridgeStarter worker;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public DotNetGatewayService()
        {
            this.ServiceName = serviceName;

            this.EventLog.Log = "Application";

            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(unhandledExceptionHandler);

        }
        public DotNetGatewayService(String[] args): this()
        {
            this.args = args;
        }
     
        public static void Main(String[] args)
        {
            //TODO Write code that could be run both as console application and as windows service
            if (Environment.UserInteractive) { Console.WriteLine(typeof(DotNetGatewayService) + " should be run as service"); return; }      
            ServiceBase.Run(new DotNetGatewayService(args));
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        protected override void OnStart(string[] args)
        {
            if (args.Length > 1) { changeStartParameters(args); this.args = args; }

            //worker = new ChangedDotNetGatewaySS(this.args);
            
            worker = new BridgeStarter(this.args);
            //worker.addLogger(new EventLogLogger(this.EventLog));

            this.bw = new BackgroundWorker();

            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            bw.RunWorkerAsync(worker);

        }

        private void changeStartParameters(string[] args)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine
                                                    .OpenSubKey("System")
                                                    .OpenSubKey("CurrentControlSet")
                                                    .OpenSubKey("Services")
                                                    .OpenSubKey(this.ServiceName, true);

            string imagePath = (string)key.GetValue("ImagePath");
            string path = imagePath.Split(' ')[0];
            key.SetValue("ImagePath", path + " " + String.Join(" ", args));

            logger.Warn("\\System\\CurrentControlSet\\Services\\" + this.ServiceName + "\\ImagePath is changed.\nOld value:" + imagePath + "\nNew value:" + key.GetValue("ImagePath"));

        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                logger.ErrorException("Stopping. Exception happened. ",e.Error);
                this.ExitCode = 1;
                this.Stop();
            }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            //((ChangedDotNetGatewaySS)(e.Argument)).processConnections();
            ((BridgeStarter)(e.Argument)).processConnections();
        }

        private void unhandledExceptionHandler(object sender, UnhandledExceptionEventArgs ue)
        {
            Exception e = (Exception)ue.ExceptionObject;
            logger.ErrorException("Stopping. Unhandled exception happened. ",e);
            this.ExitCode = 1;
            this.Stop();
        }
        //TODO Close sockets and wait for threads
        protected override void OnStop()
        {
            worker.Dispose();
            bw.CancelAsync();
            bw.Dispose();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
    }
}
