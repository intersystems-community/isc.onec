using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace isc.onec.tcp.async {
	internal sealed class SocketListenerSettings {
		/// <summary>
		/// This variable determines the number of
		/// SocketAsyncEventArg objects put in the pool of objects for receive/send.
		/// The value of this variable also affects the Semaphore.
		/// This app uses a Semaphore to ensure that the max # of connections
		/// value does not get exceeded.
		/// Max # of connections to a socket can be limited by the Windows Operating System
		/// also.
		/// </summary>
		private const Int32 _MaxConnections = 100;

		/// <summary>
		/// allows excess SAEA objects in pool.
		/// </summary>
		private const Int32 ExcessSaeaObjectsInPool = 1;

		/// <summary>
		/// The size of the queue of incoming connections for the listen socket.
		/// </summary>
		private const Int32 _Backlog = 100;

		/// <summary>
		/// This is the maximum number of asynchronous accept operations that can be
		/// posted simultaneously. This determines the size of the pool of
		/// SocketAsyncEventArgs objects that do accept operations. Note that this
		/// is NOT the same as the maximum # of connections.
		/// </summary>
		private const Int32 _MaxSimultaneousAcceptOps = 40;

		/// <summary>
		/// This number must be the same as the value on the client.
		/// Tells what size the message prefix will be. Don't change this unless
		/// you change the code, because 4 is the length of 32 bit integer, which
		/// is what we are using as prefix.
		/// </summary>
		private const Int32 _ReceivePrefixLength = 4;

		/// <summary>
		/// You would want a buffer size larger than 25 probably, unless you know the
		/// data will almost always be less than 25. It is just 25 in our test app.
		/// </summary>
		private const Int32 _ReceiveBufferSize = 128;

		private const Int32 _SendPrefixLength = 4;

		/// <summary>
		/// For the BufferManager
		/// </summary>
		private const Int32 _OpsToPreAllocate = 2; // 1 for receive, 1 for send


		// the maximum number of connections the sample is designed to handle simultaneously 
		private readonly Int32 maxConnections;

		// this variable allows us to create some extra SAEA objects for the pool,
		// if we wish.
		private readonly Int32 numberOfSaeaForRecSend;

		// max # of pending connections the listener can hold in queue
		private readonly Int32 backlog;

		// tells us how many objects to put in pool for accept operations
		private readonly Int32 maxSimultaneousAcceptOps;

		// buffer size to use for each socket receive operation
		private readonly Int32 receiveBufferSize;

		// length of message prefix for receive ops
		private readonly Int32 receivePrefixLength;

		// length of message prefix for send ops
		private readonly Int32 sendPrefixLength;

		// See comments in buffer manager.
		private readonly Int32 opsToPreAllocate;

		// Endpoint for the listener.
		private readonly IPEndPoint localEndPoint;

		internal SocketListenerSettings(IPEndPoint localEndPoint,
				Int32 maxConnections = _MaxConnections,
				Int32 numberOfSaeaForRecSend = _MaxConnections + ExcessSaeaObjectsInPool,
				Int32 backlog = _Backlog,
				Int32 maxSimultaneousAcceptOps = _MaxSimultaneousAcceptOps,
				Int32 receivePrefixLength = _ReceivePrefixLength,
				Int32 receiveBufferSize = _ReceiveBufferSize,
				Int32 sendPrefixLength = _SendPrefixLength,
				Int32 opsToPreAllocate = _OpsToPreAllocate) {
			this.maxConnections = maxConnections;
			this.numberOfSaeaForRecSend = numberOfSaeaForRecSend;
			this.backlog = backlog;
			this.maxSimultaneousAcceptOps = maxSimultaneousAcceptOps;
			this.receivePrefixLength = receivePrefixLength;
			this.receiveBufferSize = receiveBufferSize;
			this.sendPrefixLength = sendPrefixLength;
			this.opsToPreAllocate = opsToPreAllocate;
			this.localEndPoint = localEndPoint;
		}

	public Int32 MaxConnections
	{
	    get
	    {
		return this.maxConnections;
	    }
	}
	public Int32 NumberOfSaeaForRecSend
	{
	    get
	    {
		return this.numberOfSaeaForRecSend;
	    }
	}
	public Int32 Backlog
	{
	    get
	    {
		return this.backlog;
	    }
	}
	public Int32 MaxAcceptOps
	{
	    get
	    {
		return this.maxSimultaneousAcceptOps;
	    }
	}
	public Int32 ReceivePrefixLength
	{
	    get
	    {
		return this.receivePrefixLength;
	    }
	}
	public Int32 BufferSize
	{
	    get
	    {
		return this.receiveBufferSize;
	    }
	}
	public Int32 SendPrefixLength
	{
	    get
	    {
		return this.sendPrefixLength;
	    }
	}
	public Int32 OpsToPreAllocate
	{
	    get
	    {
		return this.opsToPreAllocate;
	    }
	}
	public IPEndPoint LocalEndPoint
	{
	    get
	    {
		return this.localEndPoint;
	    }
	}    
    }    
}
