using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;

namespace isc.onec.bridge
{
    //TODO add try catch
    //TODO add logging
    //TODO make tests
    //TODO test multithreading
    public class V8Adapter
    {
        private string url;
        private object connector;
        //TODO check this
        private static ReaderWriterLock connectorLock = new ReaderWriterLock();

        public bool isConnected = false;
        public bool inDisconnectingMode = false;
        public enum V8Version { V80, V81, V82 };
        //private static readonly object syncHandle = new object();

        private static Logger logger = LogManager.GetCurrentClassLogger();
        public V8Adapter()
        {
            logger.Debug("V8Adapter is created");
        }
        public object get(object target, string name)
        {
            object result;
            try
            {
                result = target.GetType().InvokeMember(name, BindingFlags.GetProperty | BindingFlags.Public, null, target, null);
            }
            catch (TargetInvocationException exception)
            {
                logger.DebugException("get", exception);
                throw exception.InnerException;
            }
            return result;
        }
        public void set(object target, string name, object value)
        {
            /*
             *  target.comObject.GetType().InvokeMember(propertyName, BindingFlags.PutDispProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, target.comObject, new object[] { propertyValue });
             */
            try
            {
                target.GetType().InvokeMember(name, BindingFlags.SetProperty | BindingFlags.Public, null, target, new object[] { value });
            }
            catch (TargetInvocationException exception)
            {
                logger.DebugException("set", exception);
                throw exception.InnerException;
            }
        }

        public object invoke(object target, string method, object[] args)
        {
            object obj;
            try
            {
                //obj2 = target.comObject.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target.comObject, methodParams, modifiers, null, null);
                // | BindingFlags.Public
                // TODO Modifiers
                obj = target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args,null,null,null);
            }
            catch (TargetInvocationException exception)
            {
                logger.DebugException("invoke", exception);
                throw exception.InnerException;
            }
            return obj;
        }

        public object connect(string url)
        {
            object context;
            try
            {
                V8Version version = getVersion(url);
                //TODO Check this
                connectorLock.AcquireReaderLock(-1);
                this.connector = createConnector(version);
                logger.Debug("New V8.ComConnector is created");
                context = invoke(this.connector, "Connect", new object[] { url });

                isConnected = true;
                this.url = url;

                logger.Debug("Connection is established");
            }
            catch (Exception e)
            {
                free(this.connector);
                stimulateGC();
                isConnected = false;
                throw e;
            }
            finally {
                //TODO Check this
                 connectorLock.ReleaseReaderLock();
            }

            return context;
        }

        public void disconnect()
        {
            if (inDisconnectingMode)
            {
                throw new InvalidOperationException("Already in disconnecting mode.");
            }
          
            free(this.connector);
            stimulateGC();
            isConnected = false;

            logger.Debug("Disconnection is done.");
            
        }

        public bool isAlive(string url)
        {
            throw new NotImplementedException();
        }
        
        public V8Version[] getInstalledVersions()
        {
            V8Version[] values = (V8Version[])Enum.GetValues(typeof(V8Version));
            List<V8Version> list = new List<V8Version>();
            foreach (V8Version version in values)
            {
                try
                {
                    createConnector(version);
                    list.Add(version);
                }
                catch
                {
                }
            }
            return list.ToArray();
        }
        private void stimulateGC()
        {
            GC.Collect();
            GC.Collect();
            //GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }
        private object createConnector(V8Version version)
        {
            string str;
            switch (version)
            {
                case V8Version.V80:
                    str = "V8.ComConnector";
                    break;

                case V8Version.V81:
                    str = "V81.ComConnector";
                    break;

                case V8Version.V82:
                    str = "V82.ComConnector";
                    break;

                default:
                    throw new NotImplementedException();
            }
            Type typeFromProgID = Type.GetTypeFromProgID(str, true);
            //this.version = version;
            return Activator.CreateInstance(typeFromProgID);
        }
        public void free(object rcw)
        {
            if (rcw != null)
            {

                logger.Debug("Releasing object " + ((MarshalByRefObject)rcw).ToString());
                Marshal.ReleaseComObject(rcw);
                Marshal.FinalReleaseComObject(rcw);
           
                rcw = null;
            }
            //
            //this.Dispose(true);
            //GC.SuppressFinalize(this);
        }
        public bool isObject(object val)
        {
            return (val is MarshalByRefObject);
        }
        private V8Version getVersion(string url)
        {
            string version = "V81";
            string[] parameters = url.Split(';');
            for (int i = 0; i < parameters.Length; i++)
            {
                string[] parameter = parameters[i].Split('=');
                if (parameter[0] == "Version")
                {
                    version = parameter[1].Trim('\"');
                }
            }

            switch (version)
            {
                case "V80":
                    return V8Version.V80;
                case "V81":
                    return V8Version.V81;
                case "V82":
                    return V8Version.V82;
                default:
                    throw new NotImplementedException("this version of 1C is not supported");
            }
        }

    }
}
