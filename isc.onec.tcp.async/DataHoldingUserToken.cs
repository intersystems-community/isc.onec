using System;
using System.Net.Sockets;
using System.Threading;
//for testing
using NLog;
using isc.onec.bridge;

namespace isc.onec.tcp.async {
	internal sealed class DataHoldingUserToken {
		internal Mediator theMediator;
		internal DataHolder theDataHolder;

		internal readonly Int32 bufferOffsetReceive;
		internal readonly Int32 permanentReceiveMessageOffset;
		internal readonly Int32 bufferOffsetSend;

		private readonly Int32 tokenId; //for testing only		

		internal Int32 lengthOfCurrentIncomingMessage;

		//receiveMessageOffset is used to mark the byte position where the message
		//begins in the receive buffer. This value can sometimes be out of
		//bounds for the data stream just received. But, if it is out of bounds, the 
		//code will not access it.
		internal Int32 receiveMessageOffset;
		internal Byte[] byteArrayForPrefix;
		internal readonly Int32 receivePrefixLength;
		internal Int32 receivedPrefixBytesDoneCount = 0;
		internal Int32 receivedMessageBytesDoneCount = 0;
		//This variable will be needed to calculate the value of the
		//receiveMessageOffset variable in one situation. Notice that the
		//name is similar but the usage is different from the variable
		//receiveSendToken.receivePrefixBytesDone.
		internal Int32 recPrefixBytesDoneThisOp = 0;

		internal Int32 sendBytesRemainingCount;
		internal readonly Int32 sendPrefixLength;
		internal Byte[] dataToSend;
		internal Int32 bytesSentAlreadyCount;

		//The session ID correlates with all the data sent in a connected session.
		//It is different from the transmission ID in the DataHolder, which relates
		//to one TCP message. A connected session could have many messages, if you
		//set up your app to allow it.
		//private Int32 sessionId;				

		public Server server;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		internal DataHoldingUserToken(SocketAsyncEventArgs e, Int32 rOffset, Int32 sOffset, Int32 receivePrefixLength, Int32 sendPrefixLength, Int32 tokenId) {
			this.tokenId = tokenId;

			//Create a Mediator that has a reference to the SAEA object.
			this.theMediator = new Mediator(e);
			this.bufferOffsetReceive = rOffset;
			this.bufferOffsetSend = sOffset;
			this.receivePrefixLength = receivePrefixLength;
			this.sendPrefixLength = sendPrefixLength;
			this.receiveMessageOffset = rOffset + receivePrefixLength;
			this.permanentReceiveMessageOffset = this.receiveMessageOffset;
		}
		~DataHoldingUserToken() {
			logger.Debug("DataHoldingUserToken destructor is called");
			this.server = null;
		}
		//Let's use an ID for this object during testing, just so we can see what
		//is happening better if we want to.
		internal Int32 TokenId {
			get {
				return this.tokenId;
			}
		}

		internal void CreateNewDataHolder() {
			theDataHolder = new DataHolder();
		}

		public void Reset() {
			this.receivedPrefixBytesDoneCount = 0;
			this.receivedMessageBytesDoneCount = 0;
			this.recPrefixBytesDoneThisOp = 0;
			this.receiveMessageOffset = this.permanentReceiveMessageOffset;
		}

		public void CleanUp() {
			logger.Debug("Cleanup is called");
			if (this.server != null) {
				this.server.Disconnect();
				this.server = null;
			}
		}

		public Server getServer() {
			return this.server;
		}

		internal void StartSession() {
			logger.Debug("Creating new isc.onec.bridge.Server");
			this.server = new Server();

		}
	}
}
