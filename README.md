# Caché 1C Bridge

## Starting a Windows service with arguments

Valid options are:

1. net start
    * Arguments should start with a forward slash
    * Arguments are passed to the OnStart(...) method
    * Changes are transient unless custom login overrides the default OnStart(...) behaviour

            net start [service name] </arg1> </arg2> ...

2. sc start
    * Arguments are passed to the OnStart(...) method
    * Changes are transient unless custom login overrides the default OnStart(...) behaviour

            sc start [service name] <arg1> <arg2> ...

3. services.msc
    * General->"Start parameters:" field (when service is stopped)
    * Arguments are passed to the OnStart(...) method
    * Changes are transient unless custom login overrides the default OnStart(...) behaviour
4. modify ImagePath registry key, either manually or programmatically.
    * Changes are permanent
5. modify registry key via sc.exe
    * Changes are permanent

            sc config "Cache One C Bridge" binPath= "\"C:\Documents and Settings\ashcheglov\My Documents\Visual Studio 2010\Projects\isc.onec\isc.gateway.net.service\bin\x86\Debug\isc.onec.service.exe\" 9101"

6. .NET-based service: create a separate **isc.onec.service.exe.config** file per service
    * Allows multiple instances, but with different executable names (so that .config files are also named differently))
    * <http://msdn.microsoft.com/en-us/library/ms229689%28v=vs.71%29.aspx>
    * <http://stackoverflow.com/questions/453161/best-practice-to-save-application-settings-in-a-windows-forms-application>
    * <http://stackoverflow.com/questions/460935/pros-and-cons-of-appsettings-vs-applicationsettings-net-app-config>
    * <http://www.codeproject.com/Articles/118532/Saving-Connection-Strings-to-app-config>
7. Custom service installer (subclass System.Configuration.Install.Installer)
    * Allows mutiple service instances referring to the same executable.
    * Changes are permanent.
    * InstallUtil.exe can accept custom arguments during service installation.
    * <http://stackoverflow.com/questions/4862580/using-installutil-to-install-a-windows-service-with-startup-parameters>
