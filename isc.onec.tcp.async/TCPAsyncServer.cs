using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using isc.general;
using NLog;

namespace isc.onec.tcp.async {
	// ____________________________________________________________________________
	// Implements the logic for the socket server.
	public sealed class TCPAsyncServer : IDisposable {
		private volatile int numberOfAcceptedSockets;

		// To keep a record of maximum number of simultaneous connections
		// that occur while the server is running. This can be limited by operating
		// system and hardware. It will not be higher than the value that you set
		// for _MaxConnections.
		private static volatile int maxSimultaneousClientsThatWereConnected = 0;

		// Buffers for sockets are unmanaged by .NET.
		// So memory used for buffers gets "pinned", which makes the
		// .NET garbage collector work around it, fragmenting the memory.
		// Circumvent this problem by putting all buffers together
		// in one block in memory. Then we will assign a part of that space
		// to each SocketAsyncEventArgs object, and
		// reuse that buffer space each time we reuse the SocketAsyncEventArgs object.
		// Create a large reusable set of buffers for all socket operations.
		private BufferManager theBufferManager;

		// the socket used to listen for incoming connection requests
		private Socket listenSocket;

		// A Semaphore has two parameters, the initial number of available slots
		// and the maximum number of slots. We'll make them the same.
		// This Semaphore is used to keep from going over max connection #. (It is not about
		// controlling threading really here.)
		private Semaphore theMaxConnectionsEnforcer;

		private SocketListenerSettings socketListenerSettings;

		// pool of reusable SocketAsyncEventArgs objects for accept operations
		private readonly SocketAsyncEventArgsPool acceptPool;

		// pool of reusable SocketAsyncEventArgs objects for receive and send socket operations
		private readonly SocketAsyncEventArgsPool sendReceivePool;

		private bool keepAlive;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		private static SocketListenerSettings getSettings(int port) {
			try {
				// Get endpoint for the listener.
				IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

				var message = "Server will listen on TCP port " + port;
				Logger.Info(message);
				EventLog.WriteEntry(message);

				// This object holds a lot of settings that we pass from Main method
				// to the SocketListener. In a real app, you might want to read
				// these settings from a database or windows registry settings that
				// you would create.
				return new SocketListenerSettings(localEndPoint);
			} catch (Exception ex) {
				Logger.ErrorException("Error on TCP server creation", ex);
				EventLog.WriteEntry(ex.ToStringWithIlOffsets(), EventLogEntryType.Error);
				return null;
			}
		}

		/// <summary>
		/// Invoked by <code>BridgeStarter</code>.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="keepAlive"></param>
		public TCPAsyncServer(int port, bool keepAlive) {
			this.keepAlive = keepAlive;

			this.socketListenerSettings = getSettings(port);
	
			// Allocate memory for buffers. We are using a separate buffer space for
			// receive and send, instead of sharing the buffer space, like the Microsoft
			// example does.
			this.theBufferManager = new BufferManager(this.socketListenerSettings.BufferSize * this.socketListenerSettings.NumberOfSaeaForRecSend * this.socketListenerSettings.OpsToPreAllocate,
			this.socketListenerSettings.BufferSize * this.socketListenerSettings.OpsToPreAllocate);

			this.sendReceivePool = new SocketAsyncEventArgsPool(this.socketListenerSettings.NumberOfSaeaForRecSend);
			this.acceptPool = new SocketAsyncEventArgsPool(this.socketListenerSettings.MaxAcceptOps);

			// Create connections count enforcer
			this.theMaxConnectionsEnforcer = new Semaphore(this.socketListenerSettings.MaxConnections, this.socketListenerSettings.MaxConnections);

			// Microsoft's example called these from Main method, which you
			// can easily do if you wish.
			this.Init();
			this.StartListen();
		}

		~TCPAsyncServer() {
			const string Message = "AsyncTCPServer exits";
			Logger.Info(Message);
			EventLog.WriteEntry(Message);
		}

		// ____________________________________________________________________________
		// initializes the server by preallocating reusable buffers and
		// context objects (SocketAsyncEventArgs objects).
		// It is NOT mandatory that you preallocate them or reuse them. But, but it is
		// done this way to illustrate how the API can
		// easily be used to create reusable objects to increase server performance.
		private void Init() {
			// Allocate one large byte buffer block, which all I/O operations will
			// use a piece of. This gaurds against memory fragmentation.
			this.theBufferManager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects for accept operations
			for (int i = 0; i < this.socketListenerSettings.MaxAcceptOps; i++) {
				// add SocketAsyncEventArg to the pool
				this.acceptPool.Push(this.CreateNewSaeaForAccept());
			}

			for (int i = 0; i < this.socketListenerSettings.NumberOfSaeaForRecSend; i++) {
				// The pool that we built ABOVE is for SocketAsyncEventArgs objects that do
				// accept operations.
				// Now we will build a separate pool for SAEAs objects
				// that do receive/send operations. One reason to separate them is that accept
				// operations do NOT need a buffer, but receive/send operations do.
				// ReceiveAsync and SendAsync require
				// a parameter for buffer size in SocketAsyncEventArgs.Buffer.
				// So, create pool of SAEA objects for receive/send operations.

				// Allocate the SocketAsyncEventArgs object for this loop,
				// to go in its place in the stack which will be the pool
				// for receive/send operation context objects.
				SocketAsyncEventArgs sendReceiveArgs = new SocketAsyncEventArgs();

				// assign a byte buffer from the buffer block to
				// this particular SocketAsyncEventArg object
				var success = this.theBufferManager.SetBuffer(sendReceiveArgs);
				if (!success) {
					EventLog.WriteEntry("TCPAsyncServer.Init(): BufferManager.SetBuffer(...) failed.", EventLogEntryType.Error);
				}

				int tokenId = this.sendReceivePool.NextTokenId + 1000000;

				// Attach the SocketAsyncEventArgs object
				// to its event handler. Since this SocketAsyncEventArgs object is
				// used for both receive and send operations, whenever either of those
				// completes, the IO_Completed method will be called.
				sendReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.IO_Completed);

				// We can store data in the UserToken property of SAEA object.
				// WTF? Three constructor parameters is enough here.
				DataHoldingUserToken theTempReceiveSendUserToken = new DataHoldingUserToken(sendReceiveArgs,
					sendReceiveArgs.Offset,
					sendReceiveArgs.Offset + this.socketListenerSettings.BufferSize,
					this.socketListenerSettings.ReceivePrefixLength,
					this.socketListenerSettings.SendPrefixLength,
					tokenId);

				// We'll have an object that we call DataHolder, that we can Remove from
				// the UserToken when we are finished with it. So, we can hang on to the
				// DataHolder, pass it to an app, serialize it, or whatever.
				theTempReceiveSendUserToken.CreateNewDataHolder();

				sendReceiveArgs.UserToken = theTempReceiveSendUserToken;

				// add this SocketAsyncEventArg object to the pool.
				this.sendReceivePool.Push(sendReceiveArgs);
			}
		}

		// ____________________________________________________________________________
		// This method is called when we need to create a new SAEA object to do
		// accept operations. The reason to put it in a separate method is so that
		// we can easily add more objects to the pool if we need to.
		// You can do that if you do NOT use a buffer in the SAEA object that does
		// the accept operations.
		private SocketAsyncEventArgs CreateNewSaeaForAccept() {
			// Allocate the SocketAsyncEventArgs object.
			SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();

			// SocketAsyncEventArgs.Completed is an event, (the only event,)
			// declared in the SocketAsyncEventArgs class.
			// See http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.completed.aspx.
			// An event handler should be attached to the event within
			// a SocketAsyncEventArgs instance when an asynchronous socket
			// operation is initiated, otherwise the application will not be able
			// to determine when the operation completes.
			// Attach the event handler, which causes the calling of the
			// AcceptEventArg_Completed object when the accept op completes.
			acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.AcceptEventArg_Completed);

			acceptEventArg.UserToken = new AcceptOpUserToken(this.acceptPool.NextTokenId + 10000);

			return acceptEventArg;

			// accept operations do NOT need a buffer.
			// You can see that is true by looking at the
			// methods in the .NET Socket class on the Microsoft website. AcceptAsync does
			// not take require a parameter for buffer size.
		}

		// ____________________________________________________________________________
		// This method starts the socket server such that it is listening for
		// incoming connection requests.
		private void StartListen() {
			// create the socket which listens for incoming connections
			this.listenSocket = new Socket(this.socketListenerSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			// bind it to the port
			this.listenSocket.Bind(this.socketListenerSettings.LocalEndPoint);

			// Start the listener with a Backlog of however many connections.
			// "Backlog" means pending connections.
			// The Backlog number is the number of clients that can wait for a
			// SocketAsyncEventArg object that will do an accept operation.
			// The listening socket keeps the Backlog as a queue. The Backlog allows
			// for a certain # of excess clients waiting to be connected.
			// If the Backlog is maxed out, then the client will receive an error when
			// trying to connect.
			// max # for Backlog can be limited by the operating system.
			this.listenSocket.Listen(this.socketListenerSettings.Backlog);
			const string Message = "Server is listening for upcoming connections.";
			Logger.Info(Message);
			EventLog.WriteEntry(Message);

			// Calls the method which will post accepts on the listening socket.
			// This call just occurs one time from this StartListen method.
			// After that the StartAccept method will be called in a loop.
			this.StartAccept();
		}

		// ____________________________________________________________________________
		// Begins an operation to accept a connection request from the client
		private void StartAccept() {
			SocketAsyncEventArgs acceptEventArg;

			// Get a SocketAsyncEventArgs object to accept the connection.
			// Get it from the pool if there is more than one in the pool.
			// We could use zero as bottom, but one is a little safer.
			if (this.acceptPool.Count > 1) {
				try {
					acceptEventArg = this.acceptPool.Pop();
				} catch {
					// or make a new one.
					Logger.Debug("no objects in pool");
					acceptEventArg = this.CreateNewSaeaForAccept();
				}
			} else {
				// or make a new one.
				acceptEventArg = this.CreateNewSaeaForAccept();
			}

			// Semaphore class is used to control access to a resource or pool of
			// resources. Enter the semaphore by calling the WaitOne method, which is
			// inherited from the WaitHandle class, and release the semaphore
			// by calling the Release method. This is a mechanism to prevent exceeding
			// the max # of connections we specified. We'll do this before
			// doing AcceptAsync. If maxConnections value has been reached,
			// then the application will pause here until the Semaphore gets released,
			// which happens in the CloseClientSocket method.
			this.theMaxConnectionsEnforcer.WaitOne();

			// Socket.AcceptAsync begins asynchronous operation to accept the connection.
			// Note the listening socket will pass info to the SocketAsyncEventArgs
			// object that has the Socket that does the accept operation.
			// If you do not create a Socket object and put it in the SAEA object
			// before calling AcceptAsync and use the AcceptSocket property to get it,
			// then a new Socket object will be created for you by .NET.
			bool willRaiseEvent = this.listenSocket.AcceptAsync(acceptEventArg);

			// Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is
			// working asynchronously. The
			// SocketAsyncEventArgs.Completed event on the acceptEventArg parameter
			// will be raised upon completion of accept op.
			// AcceptAsync will call the AcceptEventArg_Completed
			// method when it completes, because when we created this SocketAsyncEventArgs
			// object before putting it in the pool, we set the event handler to do it.
			// AcceptAsync returns false if the I/O operation completed synchronously.
			// The SocketAsyncEventArgs.Completed event on the acceptEventArg
			// parameter will NOT be raised when AcceptAsync returns false.
			if (!willRaiseEvent) {
				// The code in this if (!willRaiseEvent) statement only runs
				// when the operation was completed synchronously. It is needed because
				// when Socket.AcceptAsync returns false,
				// it does NOT raise the SocketAsyncEventArgs.Completed event.
				// And we need to call ProcessAccept and pass it the SAEA object.
				// This is only when a new connection is being accepted.
				// Probably only relevant in the case of a socket error.
				Logger.Debug("!willRaiseEvent");
				this.ProcessAccept(acceptEventArg);
			}
		}

		// ____________________________________________________________________________
		// This method is the callback method associated with Socket.AcceptAsync
		// operations and is invoked when an async accept operation completes.
		// This is only when a new connection is being accepted.
		// Notice that Socket.AcceptAsync is returning a value of true, and
		// raising the Completed event when the AcceptAsync method completes.
		private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e) {
			// Any code that you put in this method will NOT be called if
			// the operation completes synchronously, which will probably happen when
			// there is some kind of socket error. It might be better to put the code
			// in the ProcessAccept method.
			this.ProcessAccept(e);
		}

		// ____________________________________________________________________________
		// The socketAsyncEventArgs parameter passed from the AcceptEventArg_Completed method
		// represents the SocketAsyncEventArgs object that did
		// the accept operation. in this method we'll do the handoff from it to the
		// SocketAsyncEventArgs object that will do receive/send.
		private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs) {
			// This is when there was an error with the accept op. That should NOT
			// be happening often. It could indicate that there is a problem with
			// that socket. If there is a problem, then we would have an infinite
			// loop here, if we tried to reuse that same socket.
			if (acceptEventArgs.SocketError != SocketError.Success) {
				// Loop back to post another accept op. Notice that we are NOT
				// passing the SAEA object here.
				this.LoopToStartAccept();

				AcceptOpUserToken theAcceptOpToken = (AcceptOpUserToken) acceptEventArgs.UserToken;

				Logger.Error("SocketError, accept id " + theAcceptOpToken.TokenId);

				// Let's destroy this socket, since it could be bad.
				this.HandleBadAccept(acceptEventArgs);

				// Jump out of the method.
				return;
			}

			int max = maxSimultaneousClientsThatWereConnected;
			#pragma warning disable 420
			int numberOfConnectedSockets = Interlocked.Increment(ref this.numberOfAcceptedSockets);
			#pragma warning restore 420
			if (numberOfConnectedSockets > max) {
				#pragma warning disable 420
				Interlocked.Increment(ref maxSimultaneousClientsThatWereConnected);
				#pragma warning restore 420
			}

			// Now that the accept operation completed, we can start another
			// accept operation, which will do the same. Notice that we are NOT
			// passing the SAEA object here.
			this.LoopToStartAccept();

			// Get a SocketAsyncEventArgs object from the pool of receive/send op
			// SocketAsyncEventArgs objects
			SocketAsyncEventArgs receiveSendEventArgs = this.sendReceivePool.Pop();

			// Create sessionId in UserToken.
//			((DataHoldingUserToken) receiveSendEventArgs.UserToken).CreateSessionId();
			((DataHoldingUserToken) receiveSendEventArgs.UserToken).StartSession();

			// A new socket was created by the AcceptAsync method. The
			// SocketAsyncEventArgs object which did the accept operation has that
			// socket info in its AcceptSocket property. Now we will give
			// a reference for that socket to the SocketAsyncEventArgs
			// object which will do receive/send.
			receiveSendEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;
			if (this.keepAlive) {
				SetDesiredKeepAlive(receiveSendEventArgs.AcceptSocket);
				Logger.Debug("KeepAlive is On");
			}

			// We have handed off the connection info from the
			// accepting socket to the receiving socket. So, now we can
			// put the SocketAsyncEventArgs object that did the accept operation
			// back in the pool for them. But first we will clear
			// the socket info from that object, so it will be
			// ready for a new socket when it comes out of the pool.
			acceptEventArgs.AcceptSocket = null;
			this.acceptPool.Push(acceptEventArgs);
			this.StartReceive(receiveSendEventArgs);
		}

		// ____________________________________________________________________________
		// LoopToStartAccept method just sends us back to the beginning of the
		// StartAccept method, to start the next accept operation on the next
		// connection request that this listening socket will pass of to an
		// accepting socket. We do NOT actually need this method. You could
		// just call StartAccept() in ProcessAccept() where we called LoopToStartAccept().
		// This method is just here to help you visualize the program flow.
		private void LoopToStartAccept() {
			this.StartAccept();
		}

		// ____________________________________________________________________________
		// Set the receive buffer and post a receive op.
		private void StartReceive(SocketAsyncEventArgs receiveSendEventArgs) {
			DataHoldingUserToken receiveSendToken = (DataHoldingUserToken) receiveSendEventArgs.UserToken;

			// Set the buffer for the receive operation.
			receiveSendEventArgs.SetBuffer(receiveSendToken.BufferOffsetReceive, this.socketListenerSettings.BufferSize);

			// Post async receive operation on the socket.
			Socket socket = receiveSendEventArgs.AcceptSocket;
			bool willRaiseEvent;
			try {
				willRaiseEvent = socket.ReceiveAsync(receiveSendEventArgs);
			} catch (Exception e) {
				willRaiseEvent = false;
				Logger.ErrorException("socket.ReceiveAsync:", e);
				EventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Error);
			}

			// Socket.ReceiveAsync returns true if the I/O operation is pending. The
			// SocketAsyncEventArgs.Completed event on the e parameter will be raised
			// upon completion of the operation. So, true will cause the IO_Completed
			// method to be called when the receive operation completes.
			// That's because of the event handler we created when building
			// the pool of SocketAsyncEventArgs objects that perform receive/send.
			// It was the line that said
////			socketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

			// Socket.ReceiveAsync returns false if I/O operation completed synchronously.
			// In that case, the SocketAsyncEventArgs.Completed event on the e parameter
			// will not be raised and the e object passed as a parameter may be
			// examined immediately after the method call
			// returns to retrieve the result of the operation.
			// It may be false in the case of a socket error.
			if (!willRaiseEvent) {
				// If the op completed synchronously, we need to call ProcessReceive
				// method directly. This will probably be used rarely, as you will
				// see in testing.
				this.ProcessReceive(receiveSendEventArgs);
			}
		}

		// ____________________________________________________________________________
		// This method is called whenever a receive or send operation completes.
		// Here "e" represents the SocketAsyncEventArgs object associated
		// with the completed receive or send operation
		private void IO_Completed(object sender, SocketAsyncEventArgs e) {
			// Any code that you put in this method will NOT be called if
			// the operation completes synchronously, which will probably happen when
			// there is some kind of socket error.

			// determine which type of operation just completed and call the associated handler
			switch (e.LastOperation) {
			case SocketAsyncOperation.Receive:
				this.ProcessReceive(e);
				break;
			case SocketAsyncOperation.Send:
				this.ProcessSend(e);
				break;
			default:
				// This exception will occur if you code the Completed event of some
				// operation to come to this method, by mistake.
				Logger.Error("IO_Completed(): The last operation completed on the socket was not a receive or send");
				throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			}
		}

		// ____________________________________________________________________________
		// This method is invoked by the IO_Completed method
		// when an asynchronous receive operation completes.
		// If the remote host closed the connection, then the socket is closed.
		// Otherwise, we process the received data. And if a complete message was
		// received, then we do some additional processing, to
		// respond to the client.
		private void ProcessReceive(SocketAsyncEventArgs receiveSendEventArgs) {
			DataHoldingUserToken receiveSendToken = (DataHoldingUserToken) receiveSendEventArgs.UserToken;

			// If there was a socket error, close the connection. This is NOT a normal
			// situation, if you get an error here.
			// In the Microsoft example code they had this error situation handled
			// at the end of ProcessReceive. Putting it here improves readability
			// by reducing nesting some.
			if (receiveSendEventArgs.SocketError != SocketError.Success) {
				receiveSendToken.Reset();
				this.CloseClientSocket(receiveSendEventArgs);

				// Jump out of the ProcessReceive method.
				return;
			}

			// If no data was received, close the connection. This is a NORMAL
			// situation that shows when the client has finished sending data.
			if (receiveSendEventArgs.BytesTransferred == 0) {
				receiveSendToken.Reset();
				this.CloseClientSocket(receiveSendEventArgs);
				return;
			}

			// The BytesTransferred property tells us how many bytes
			// we need to process.
			int remainingBytesToProcess = receiveSendEventArgs.BytesTransferred;

			// If we have not got all of the prefix already,
			// then we need to work on it here.
			if (receiveSendToken.ReceivedPrefixBytesDoneCount < this.socketListenerSettings.ReceivePrefixLength) {
				remainingBytesToProcess = PrefixHandler.HandlePrefix(receiveSendEventArgs, receiveSendToken, remainingBytesToProcess);

				if (remainingBytesToProcess == 0) {
					// We need to do another receive op, since we do not have
					// the message yet, but remainingBytesToProcess == 0.
					this.StartReceive(receiveSendEventArgs);

					// Jump out of the method.
					return;
				}
			}

			// If we have processed the prefix, we can work on the message now.
			// We'll arrive here when we have received enough bytes to read
			// the first byte after the prefix.
			bool incomingTcpMessageIsReady = false;
			try {
				incomingTcpMessageIsReady = MessageHandler.HandleMessage(receiveSendEventArgs.Buffer, receiveSendToken, remainingBytesToProcess);
			} catch (Exception e) {
				try {
					Logger.ErrorException("Weird error:", e);
					EventLog.WriteEntry(e.ToStringWithIlOffsets(), EventLogEntryType.Error);
					receiveSendToken.DataHolder.IsError = true;
					receiveSendToken.Mediator.HandleData(receiveSendToken.DataHolder);

					receiveSendToken.CreateNewDataHolder();

					receiveSendToken.Mediator.PrepareOutgoingData();
					this.StartSend(receiveSendToken.Mediator.SocketAsyncEventArgs);
				} catch (Exception ex) {
					Logger.ErrorException("on recovery from error:", ex);
					EventLog.WriteEntry(ex.ToStringWithIlOffsets(), EventLogEntryType.Error);
				}

				receiveSendToken.CleanUp();
				receiveSendToken.Reset();
////				CloseClientSocket(receiveSendEventArgs);
				return;
			}

			if (incomingTcpMessageIsReady) {
				// Pass the DataHolder object to the Mediator here. The data in
				// this DataHolder can be used for all kinds of things that an
				// intelligent and creative person like you might think of.
				receiveSendToken.Mediator.HandleData(receiveSendToken.DataHolder);

				// Create a new DataHolder for next message.
				receiveSendToken.CreateNewDataHolder();

				// Reset the variables in the UserToken, to be ready for the
				// next message that will be received on the socket in this
				// SAEA object.
				receiveSendToken.Reset();

				receiveSendToken.Mediator.PrepareOutgoingData();
				this.StartSend(receiveSendToken.Mediator.SocketAsyncEventArgs);
			} else {
				// Since we have NOT gotten enough bytes for the whole message,
				// we need to do another receive op. Reset some variables first.

				// All of the data that we receive in the next receive op will be
				// message. None of it will be prefix. So, we need to move the
				// receiveSendToken.receiveMessageOffset to the beginning of the
				// receive buffer space for this SAEA.
				receiveSendToken.ReceiveMessageOffset = receiveSendToken.BufferOffsetReceive;

				// Do NOT reset receiveSendToken.receivedPrefixBytesDoneCount here.
				// Just reset recPrefixBytesDoneThisOp.
				receiveSendToken.RecPrefixBytesDoneThisOp = 0;
				this.StartReceive(receiveSendEventArgs);
			}
		}

		// ____________________________________________________________________________
		// Post a send.
		private void StartSend(SocketAsyncEventArgs receiveSendEventArgs) {
			DataHoldingUserToken receiveSendToken = (DataHoldingUserToken) receiveSendEventArgs.UserToken;

			// Set the buffer. You can see on Microsoft's page at
			// http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.setbuffer.aspx
			// that there are two overloads. One of the overloads has 3 parameters.
			// When setting the buffer, you need 3 parameters the first time you set it,
			// which we did in the Init method. The first of the three parameters
			// tells what byte array to use as the buffer. After we tell what byte array
			// to use we do not need to use the overload with 3 parameters any more.
			// (That is the whole reason for using the buffer block. You keep the same
			// byte array as buffer always, and keep it all in one block.)
			// Now we use the overload with two parameters. We tell
			// (1) the offset and
			// (2) the number of bytes to use, starting at the offset.

			// The number of bytes to send depends on whether the message is larger than
			// the buffer or not. If it is larger than the buffer, then we will have
			// to post more than one send operation. If it is less than or equal to the
			// size of the send buffer, then we can accomplish it in one send op.
			if (receiveSendToken.SendBytesRemainingCount <= this.socketListenerSettings.BufferSize) {
				receiveSendEventArgs.SetBuffer(receiveSendToken.BufferOffsetSend, receiveSendToken.SendBytesRemainingCount);

				// Copy the bytes to the buffer associated with this SAEA object.
				Buffer.BlockCopy(receiveSendToken.DataToSend, receiveSendToken.BytesSentAlreadyCount, receiveSendEventArgs.Buffer, receiveSendToken.BufferOffsetSend, receiveSendToken.SendBytesRemainingCount);
			} else {
				// We cannot try to set the buffer any larger than its size.
				// So since receiveSendToken.sendBytesRemainingCount > BufferSize, we just
				// set it to the maximum size, to send the most data possible.
				receiveSendEventArgs.SetBuffer(receiveSendToken.BufferOffsetSend, this.socketListenerSettings.BufferSize);

				// Copy the bytes to the buffer associated with this SAEA object.
				Buffer.BlockCopy(receiveSendToken.DataToSend, receiveSendToken.BytesSentAlreadyCount, receiveSendEventArgs.Buffer, receiveSendToken.BufferOffsetSend, this.socketListenerSettings.BufferSize);

				// We'll change the value of sendUserToken.sendBytesRemainingCount
				// in the ProcessSend method.
			}

			// post asynchronous send operation
			bool willRaiseEvent = receiveSendEventArgs.AcceptSocket.SendAsync(receiveSendEventArgs);

			if (!willRaiseEvent) {
				this.ProcessSend(receiveSendEventArgs);
			}
		}

		// ____________________________________________________________________________
		// This method is called by I/O Completed() when an asynchronous send completes.
		// If all of the data has been sent, then this method calls StartReceive
		// to start another receive op on the socket to read any additional
		// data sent from the client. If all of the data has NOT been sent, then it
		// calls StartSend to send more data.
		private void ProcessSend(SocketAsyncEventArgs receiveSendEventArgs) {
			DataHoldingUserToken receiveSendToken = (DataHoldingUserToken) receiveSendEventArgs.UserToken;

			if (receiveSendEventArgs.SocketError == SocketError.Success) {
				receiveSendToken.SendBytesRemainingCount = receiveSendToken.SendBytesRemainingCount - receiveSendEventArgs.BytesTransferred;

				if (receiveSendToken.SendBytesRemainingCount == 0) {
					// If we are within this if-statement, then all the bytes in
					// the message have been sent.
					this.StartReceive(receiveSendEventArgs);
				} else {
					// If some of the bytes in the message have NOT been sent,
					// then we will need to post another send operation, after we store
					// a count of how many bytes that we sent in this send op.
					receiveSendToken.BytesSentAlreadyCount += receiveSendEventArgs.BytesTransferred;

					// So let's loop back to StartSend().
					this.StartSend(receiveSendEventArgs);
				}
			} else {
				// If we are in this else-statement, there was a socket error.

				// We'll just close the socket if there was a
				// socket error when receiving data from the client.
				receiveSendToken.Reset();
				this.CloseClientSocket(receiveSendEventArgs);
			}
		}

		// ____________________________________________________________________________
		// Does the normal destroying of sockets after
		// we finish receiving and sending on a connection.
		private void CloseClientSocket(SocketAsyncEventArgs e) {
			Logger.Debug("CloseClientSocket.");
			var receiveSendToken = e.UserToken as DataHoldingUserToken;

			// do a shutdown before you close the socket
			try {
				e.AcceptSocket.Shutdown(SocketShutdown.Both);
			} catch (Exception ex) {
				// throws if socket was already closed
				Logger.ErrorException("CloseClientSocket():", ex);
				EventLog.WriteEntry(ex.ToStringWithIlOffsets(), EventLogEntryType.Error);
			}

			// This method closes the socket and releases all resources, both
			// managed and unmanaged. It internally calls Dispose.
			e.AcceptSocket.Close();

			// Make sure the new DataHolder has been created for the next connection.
			// If it has, then dataMessageReceived should be null.
			if (receiveSendToken.DataHolder.DataMessageReceived != null) {
				receiveSendToken.CreateNewDataHolder();
			}

			// Put the SocketAsyncEventArg back into the pool,
			// to be used by another client. This
			this.sendReceivePool.Push(e);

			// decrement the counter keeping track of the total number of clients
			// connected to the server, for testing
			#pragma warning disable 420
			Interlocked.Decrement(ref this.numberOfAcceptedSockets);
			#pragma warning restore 420

			Logger.Debug("Cleaning data holder");
			receiveSendToken.CleanUp();
			Logger.Debug(receiveSendToken.TokenId + " disconnected. " + this.numberOfAcceptedSockets + " client(s) connected.");

			// Release Semaphore so that its connection counter will be decremented.
			// This must be done AFTER putting the SocketAsyncEventArg back into the pool,
			// or you can run into problems.
			this.theMaxConnectionsEnforcer.Release();
		}

		// ____________________________________________________________________________
		private void HandleBadAccept(SocketAsyncEventArgs acceptEventArgs) {
			var acceptOpToken = acceptEventArgs.UserToken as AcceptOpUserToken;
			Logger.Debug("HandleBadAccept(): Closing socket of accept id " + acceptOpToken.TokenId);

			// This method closes the socket and releases all resources, both
			// managed and unmanaged. It internally calls Dispose.
			acceptEventArgs.AcceptSocket.Close();

			// Put the SAEA back in the pool.
			this.acceptPool.Push(acceptEventArgs);
		}

		private static void SetDesiredKeepAlive(Socket socket) {
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			const uint Time = 2000;
			const uint Interval = 1000;
			SetKeepAlive(socket, true, Time, Interval);
		}

		private static void SetKeepAlive(Socket s, bool on, uint time, uint interval) {
			/* 
			 * the native structure:
			 */
////			struct tcp_keepalive {
////				ULONG onoff;
////				ULONG keepalivetime;
////				ULONG keepaliveinterval;
////			};

			// marshal the equivalent of the native structure into a byte array
			uint dummy = 0;
			var inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
			BitConverter.GetBytes((uint) (on ? 1 : 0)).CopyTo(inOptionValues, 0);
			BitConverter.GetBytes((uint) time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
			BitConverter.GetBytes((uint) interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

			// call WSAIoctl via IOControl
			// Return value ignored.
			s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
		}

		/// <summary>
		/// <see cref = "System.IDisposable.Dispose()"/>
		/// </summary>
		public void Dispose() {
			this.listenSocket.Dispose();
			this.theMaxConnectionsEnforcer.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
