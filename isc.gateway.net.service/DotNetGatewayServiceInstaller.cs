using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NLog;

namespace isc.gateway.net {
	[RunInstaller(true)]
	public sealed class DotNetGatewayServiceInstaller : Installer {
		private const string DisplayName = "Caché One C Bridge";

		private const string ParameterPort = "port";

		private const string ErrorPortMissing = "Missing parameter: \"port\" (use /port=[port number] switch).";

		private readonly ServiceInstaller serviceInstaller;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public DotNetGatewayServiceInstaller() {
			ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
			this.serviceInstaller = new ServiceInstaller();
			this.serviceInstaller.StartType = ServiceStartMode.Automatic;
			/*
			 * Template value for debug purposes.
			 */
			this.serviceInstaller.ServiceName = DotNetGatewayService.ServiceNameTemplate;

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
				const string Message = ErrorPortMissing;
				WriteLine(Message, ConsoleColor.Red);
				throw new InstallException(Message);
			}
			try {
				var port = Convert.ToInt32(portString);
				this.serviceInstaller.ServiceName = DotNetGatewayService.ServiceNameTemplate + ' ' + port;
				this.serviceInstaller.DisplayName = DisplayName + ' ' + port;
				this.serviceInstaller.Description = "Runs Caché .NET Gateway at TCP port " + port;
				base.Install(stateSaver);

				/*
				 * Once the service is installed,
				 * update the ImagePath registry key with the port information.
				 */
				ChangeStartParameters(this.serviceInstaller.ServiceName, new string[] { portString });

				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" installed successfully.", ConsoleColor.Green);
			} catch (Exception e) {
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" failed to install.", ConsoleColor.Red);
				WriteLine(e.Message, ConsoleColor.Red);
				throw;
			}
		}

		public override void Uninstall(IDictionary savedState) {
			var portString = this.Context.Parameters[ParameterPort];
			if (portString == null || portString.Length == 0) {
				const string Message = ErrorPortMissing;
				WriteLine(Message, ConsoleColor.Red);
				throw new InstallException(Message);
			}
			try {
				var port = Convert.ToInt32(portString);
				this.serviceInstaller.ServiceName = DotNetGatewayService.ServiceNameTemplate + ' ' + port;
				base.Uninstall(savedState);
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" uninstalled successfully.", ConsoleColor.Green);
			} catch (Exception e) {
				WriteLine("Service \"" + this.serviceInstaller.ServiceName + "\" failed to uninstall.", ConsoleColor.Red);
				WriteLine(e.Message, ConsoleColor.Red);
				throw;
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="serviceName"></param>
		/// <param name="args"></param>
		private static void ChangeStartParameters(string serviceName, string[] args) {
			var key = Registry.LocalMachine
					.OpenSubKey("System")
					.OpenSubKey("CurrentControlSet")
					.OpenSubKey("Services")
					.OpenSubKey(serviceName, true);
			const string SubKey = "ImagePath";
			var imagePath = (string) key.GetValue(SubKey);

			/*-
			 * Group #1 is the path to EXE.
			 * Group #3 (if any) is the port number.
			 * Group #5 (if any) is the keepalive flag.
			 */
			const string ImagePathPattern = "^\\\"?([^\\\"]+\\.[Ee][Xx][Ee])\\\"?(\\s+(\\d+)(\\s+([Tt][Rr][Uu][Ee]|[Ff][Aa][Ll][Ss][Ee]))?)?\\s*$";
			if (!Regex.IsMatch(imagePath, ImagePathPattern)) {
				/*
				 * Never.
				 */
				WriteLine("ImagePath doesn't match the pattern.", ConsoleColor.Red);
				return;
			}

			var match = Regex.Match(imagePath, ImagePathPattern);
			if (match.Groups.Count < 2) {
				/*
				 * Never.
				 */
				WriteLine("Matcher is missing the group #1.", ConsoleColor.Red);
				return;
			}

			key.SetValue(SubKey, '"' + match.Groups[1].Value + "\" " + string.Join(" ", args));

			Logger.Warn("\\System\\CurrentControlSet\\Services\\" + serviceName + "\\ImagePath is changed.\nOld value:" + imagePath + "\nNew value:" + key.GetValue(SubKey));
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
