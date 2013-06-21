using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NLog;
using isc.onec.bridge;
using System.Text;

namespace isc.onec.tcp
{
    public sealed class TcpServer : IDisposable
    {
        public event EventHandler<IPEndPointEventArgs> SocketConnected;
        public event EventHandler<IPEndPointEventArgs> SocketDisconnected;
        public event EventHandler<IPEndPointEventArgs> SocketError;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        private static readonly object syncHandle = new object();
        private bool isKeepAlive = true;
        private const int SocketBufferSize = 8192;
        private readonly TcpListener tcpServer;
        private bool disposed;
        private readonly Dictionary<IPEndPoint, Socket> connectedSockets;
        private readonly object connectedSocketsSyncHandle = new object();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public TcpServer(int port, bool isKeepAlive):this(port)
        {
            this.isKeepAlive = isKeepAlive;

        }
        public TcpServer(int port)
        {
            connectedSockets = new Dictionary<IPEndPoint, Socket>();
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            tcpServer.BeginAcceptSocket(EndAcceptSocket, tcpServer);

            logger.Info("TCPServer ver. 0.8");
            logger.Info("SocketBifferSize:"+SocketBufferSize);
        }
        ~TcpServer()
        {
            DisposeImpl(false);
        }
        public void Dispose()
        {
            DisposeImpl(true);
        }

        public void SendData(byte[] data, IPEndPoint endPoint)
        {
            Socket sock;
            lock (syncHandle)
            {
                if (!connectedSockets.ContainsKey(endPoint))
                    return;
                sock = connectedSockets[endPoint];
            }
            sock.Send(data);
        }

        //Обработка нового соединения
        private void Connected(Socket socket)
        {
            var endPoint = (IPEndPoint)socket.RemoteEndPoint;

            lock (connectedSocketsSyncHandle)
            {
                if (connectedSockets.ContainsKey(endPoint))
                {
                    logger.Error("TcpServer.Connected: Socket already connected! Removing from local storage! EndPoint: {0}", endPoint);
                    connectedSockets[endPoint].Close();
                }

                if (this.isKeepAlive)
                {
                    SetDesiredKeepAlive(socket);
                    logger.Debug("KeepAlive is On");
                }
                else
                {
                    logger.Debug("KeepAlive is Off");
                }
                connectedSockets[endPoint] = socket;
            }

            OnSocketConnected(endPoint);
        }

        private static void SetDesiredKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            const uint time = 2000;
            const uint interval = 1000;
            SetKeepAlive(socket, true, time, interval);
        }
        static void SetKeepAlive(Socket s, bool on, uint time, uint interval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            var inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            // call WSAIoctl via IOControl
            int ignore = s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

        }
        //socket disconnected handler
        private void Disconnect(Socket socket)
        {
            var endPoint = (IPEndPoint)socket.RemoteEndPoint;

            lock (connectedSocketsSyncHandle)
            {
                connectedSockets.Remove(endPoint);
            }

            socket.Close();

            OnSocketDisconnected(endPoint);
        }

        private void ReceiveData(byte[] data, IPEndPoint endPoint)
        {
            OnDataReceived(data, endPoint);
        }

        private void EndAcceptSocket(IAsyncResult asyncResult)
        {
            var lister = (TcpListener)asyncResult.AsyncState;
            //Console.WriteLine("TcpServer.EndAcceptSocket");
            if (disposed)
            {
                logger.Warn("TcpServer.EndAcceptSocket: tcp server already disposed!");
                return;
            }

            try
            {
                Socket sock;
                try
                {
                    sock = lister.EndAcceptSocket(asyncResult);
                    //Console.WriteLine("TcpServer.EndAcceptSocket: remote end point: {0}", sock.RemoteEndPoint);
                    Connected(sock);
                }
                finally
                {
                    //EndAcceptSocket can failes, but in any case we want to accept new connections
                    lister.BeginAcceptSocket(EndAcceptSocket, lister);
                }
                //logger.Debug(sock.R

                var e = new SocketAsyncEventArgs();
                e.Completed += ReceiveCompleted;
                
                e.SetBuffer(new byte[SocketBufferSize], 0, SocketBufferSize);
                //int size = e.BytesTransferred;
                //e.SetBuffer(new byte[size], 0, size);
                logger.Debug("before BeginReceiveAsync");
                logger.Debug("sock.recieveBuffer:" + sock.ReceiveBufferSize);
                BeginReceiveAsync(sock, e);

            }
            catch (SocketException ex)
            {
                logger.ErrorException("TcpServer.EndAcceptSocket: failes!",ex);
            }
            catch (Exception ex)
            {
                logger.ErrorException("TcpServer.EndAcceptSocket: failes!",ex);
            }
        }

        private void BeginReceiveAsync(Socket sock, SocketAsyncEventArgs e)
        {
            if (!sock.ReceiveAsync(e))
            {
                ReceiveCompleted(sock, e);
            }
        }

        void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            logger.Debug("RecieveCompleted():");
            var sock = (Socket)sender;
            var endpoint = (IPEndPoint)sock.RemoteEndPoint;
            if (!sock.Connected)
            {
                Disconnect(sock);
                //Console.WriteLine("Socket IS Diconnected()");
                return;
            }
            try
            {

                int size = e.BytesTransferred;
                logger.Debug("Bytes transferred:" + size);
                if (size == 0)
                {
                    //this implementation based on IO Completion ports, and in this case
                    //receiving zero bytes mean socket disconnection
                    //Console.WriteLine("Trying to Diconnect Socket");
                    Disconnect(sock);
                }
                else
                {
                    var buf = new byte[size];
                    Array.Copy(e.Buffer, buf, size);
                    ReceiveData(buf, (IPEndPoint)sock.RemoteEndPoint);
                    BeginReceiveAsync(sock, e);
                }
            }
            catch (SocketException ex)
            {
                //We can't truly handle this excpetion here, but unhandled
                //exception caused process termination.
                //You can add new event to notify observer
                logger.ErrorException("TcpServer: receive data error",ex);
                OnSocketError(endpoint);
            }
            catch (Exception ex)
            {
                logger.ErrorException("TcpServer: receive data error", ex);
                OnSocketError(endpoint);
            }
        }

        private void DisposeImpl(bool manualDispose)
        {
            if (manualDispose)
            {
                //We should manually close all connected sockets
                Exception error = null;
                try
                {
                    if (tcpServer != null)
                    {
                        disposed = true;
                        tcpServer.Stop();
                    }
                }
                catch (Exception ex)
                {
                    
                    logger.ErrorException("TcpServer: tcpServer.Stop() failes!", ex);
                    error = ex;
                }

                try
                {
                    foreach (var sock in connectedSockets.Values)
                    {
                        sock.Close();
                    }
                }
                catch (SocketException ex)
                {
                    //During one socket disconnected we can faced exception
                    logger.ErrorException("TcpServer: close accepted socket failes!", ex);

                    error = ex;
                }
                if (error != null)
                    throw error;
            }
        }


        private void OnSocketConnected(IPEndPoint ipEndPoint)
        {
            var handler = SocketConnected;
            if (handler != null)
                handler(this, new IPEndPointEventArgs(ipEndPoint));
        }

        private void OnSocketDisconnected(IPEndPoint ipEndPoint)
        {
            var handler = SocketDisconnected;
            if (handler != null)
                handler(this, new IPEndPointEventArgs(ipEndPoint));
        }
        private void OnDataReceived(byte[] data, IPEndPoint ipEndPoint)
        {
            var handler = DataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs(data, ipEndPoint));
        }

        private void OnSocketError(IPEndPoint ipEndPoint)
        {
            var handler = SocketError;
            if (handler != null)
                handler(this, new IPEndPointEventArgs(ipEndPoint));
        }
     
    }

    public class IPEndPointEventArgs : EventArgs
    {


        public IPEndPointEventArgs(IPEndPoint ipEndPoint)
        {
            IPEndPoint = ipEndPoint;
        }

        public IPEndPoint IPEndPoint { get; private set; }
    }

    public class DataReceivedEventArgs : EventArgs
    {

        public DataReceivedEventArgs(byte[] data, IPEndPoint ipEndPoint)
        {
            Data = data;
            IPEndPoint = ipEndPoint;
        }

        public byte[] Data { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }

    }
}