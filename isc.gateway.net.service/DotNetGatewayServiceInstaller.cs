using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace isc.gateway.net
{
    [RunInstaller(true)]
    public class DotNetGatewayServiceInstaller : Installer
    {

        public DotNetGatewayServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();


            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Context = new InstallContext();

            serviceInstaller.DisplayName = DotNetGatewayService.serviceName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.Description = "Runs .Net Gateway at specific TCP port";
            Console.WriteLine("serviceInstaller:" + serviceInstaller.ToString());



            serviceInstaller.ServiceName = DotNetGatewayService.serviceName;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }


    }
}
