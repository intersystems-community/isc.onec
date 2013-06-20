    private Socket ListenAndBind(IPEndPoint localEndpoint)
    {
        Socket socket = new Socket(localEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.Bind(localEndpoint);
        }
        catch (SocketException exception)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(SocketConnectionListener.ConvertListenException(exception, localEndpoint));
        }
        return socket;
    }
