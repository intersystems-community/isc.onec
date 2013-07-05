using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace isc.gateway.net
{
    [RunInstaller(true)]
    public class DotNetGatewayServiceInstaller : Installer
    {
		/// <summary>
		/// Must be pure ASCII, no acute allowed.
		/// </summary>
		private const string ServiceName = "Cache One C Bridge";

		private const string DisplayName = "Caché One C Bridge";

		private const string ParameterPort = "port";

		private const string ErrorPortMissing = "Missing parameter: \"port\" (use /port=[port number] switch).";

		private readonly ServiceInstaller serviceInstaller;

        public DotNetGatewayServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
		this.serviceInstaller = new ServiceInstaller();
		this.serviceInstaller.StartType = ServiceStartMode.Automatic;
		/*
		 * Template value for debug purposes.
		 */
		this.serviceInstaller.ServiceName = ServiceName;

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Context = new InstallContext();

		this.Installers.Add(serviceProcessInstaller);
		this.Installers.Add(this.serviceInstaller);
        }

		public override void Install(IDictionary stateSaver) {
			var portString = this.Context.Parameters[ParameterPort];
			if (portString == null || portString.Length == 0) {
				const string message = ErrorPortMissing;
				WriteLine(message, ConsoleColor.Red);
				throw new InstallException(message);
			}
			try {
				var port = Convert.ToInt32(portString);
				this.serviceInstaller.ServiceName = ServiceName + ' ' + port;
				this.serviceInstaller.DisplayName = DisplayName + ' ' + port;
				this.serviceInstaller.Description = "Runs Caché .NET Gateway at TCP port " + port;
				base.Install(stateSaver);

				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" installed successfully.", ConsoleColor.Green);
			} catch (Exception e) {
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" failed to install.", ConsoleColor.Red);
				WriteLine(e.Message, ConsoleColor.Red);
				throw e;
			}
		}

		public override void Uninstall(IDictionary savedState) {
			var portString = this.Context.Parameters[ParameterPort];
			if (portString == null || portString.Length == 0) {
				const string message = ErrorPortMissing;
				WriteLine(message, ConsoleColor.Red);
				throw new InstallException(message);
			}
			try {
				var port = Convert.ToInt32(portString);
				this.serviceInstaller.ServiceName = ServiceName + ' ' + port;
				base.Uninstall(savedState);
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" uninstalled successfully.", ConsoleColor.Green);
			} catch (Exception e) {
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" failed to uninstall.", ConsoleColor.Red);
				WriteLine(e.Message, ConsoleColor.Red);
				throw e;
			}
		}

		private static void WriteLine(string message, ConsoleColor color) {
			var oldColor = Console.ForegroundColor;
			try {
				Console.ForegroundColor = color;
				Console.WriteLine(message);
			} finally {
				Console.ForegroundColor = oldColor;
			}
		}
	}
}
