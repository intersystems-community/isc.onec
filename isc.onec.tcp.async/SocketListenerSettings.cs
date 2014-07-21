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
		private const int DefaultMaxConnections = 100;

		/// <summary>
		/// allows excess SAEA objects in pool.
		/// </summary>
		private const int ExcessSaeaObjectsInPool = 1;

		/// <summary>
		/// The size of the queue of incoming connections for the listen socket.
		/// </summary>
		private const int DefaultBacklog = 100;

		/// <summary>
		/// This is the maximum number of asynchronous accept operations that can be
		/// posted simultaneously. This determines the size of the pool of
		/// SocketAsyncEventArgs objects that do accept operations. Note that this
		/// is NOT the same as the maximum # of connections.
		/// </summary>
		private const int DefaultMaxSimultaneousAcceptOps = 40;

		/// <summary>
		/// This number must be the same as the value on the client.
		/// Tells what size the message prefix will be. Don't change this unless
		/// you change the code, because 4 is the length of 32 bit integer, which
		/// is what we are using as prefix.
		/// </summary>
		private const int DefaultReceivePrefixLength = 4;

		/// <summary>
		/// You would want a buffer size larger than 25 probably, unless you know the
		/// data will almost always be less than 25. It is just 25 in our test app.
		/// </summary>
		private const int DefaultReceiveBufferSize = 128;

		private const int DefaultSendPrefixLength = 4;

		/// <summary>
		/// For the BufferManager
		/// </summary>
		private const int DefaultOpsToPreAllocate = 2; // 1 for receive, 1 for send

		// the maximum number of connections the sample is designed to handle simultaneously 
		private readonly int maxConnections;

		// this variable allows us to create some extra SAEA objects for the pool,
		// if we wish.
		private readonly int numberOfSaeaForRecSend;

		// max # of pending connections the listener can hold in queue
		private readonly int backlog;

		// tells us how many objects to put in pool for accept operations
		private readonly int maxSimultaneousAcceptOps;

		// buffer size to use for each socket receive operation
		private readonly int receiveBufferSize;

		// length of message prefix for receive ops
		private readonly int receivePrefixLength;

		// length of message prefix for send ops
		private readonly int sendPrefixLength;

		// See comments in buffer manager.
		private readonly int opsToPreAllocate;

		// Endpoint for the listener.
		private readonly IPEndPoint localEndPoint;

		internal SocketListenerSettings(IPEndPoint localEndPoint,
				int maxConnections = DefaultMaxConnections,
				int numberOfSaeaForRecSend = DefaultMaxConnections + ExcessSaeaObjectsInPool,
				int backlog = DefaultBacklog,
				int maxSimultaneousAcceptOps = DefaultMaxSimultaneousAcceptOps,
				int receivePrefixLength = DefaultReceivePrefixLength,
				int receiveBufferSize = DefaultReceiveBufferSize,
				int sendPrefixLength = DefaultSendPrefixLength,
				int opsToPreAllocate = DefaultOpsToPreAllocate) {
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

		public int MaxConnections
	{
	    get
	    {
		return this.maxConnections;
	    }
	}

	public int NumberOfSaeaForRecSend
	{
	    get
	    {
		return this.numberOfSaeaForRecSend;
	    }
	}

	public int Backlog
	{
	    get
	    {
		return this.backlog;
	    }
	}

	public int MaxAcceptOps
	{
	    get
	    {
		return this.maxSimultaneousAcceptOps;
	    }
	}

	public int ReceivePrefixLength
	{
	    get
	    {
		return this.receivePrefixLength;
	    }
	}

	public int BufferSize
	{
	    get
	    {
		return this.receiveBufferSize;
	    }
	}

	public int SendPrefixLength
	{
	    get
	    {
		return this.sendPrefixLength;
	    }
	}

	public int OpsToPreAllocate
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
