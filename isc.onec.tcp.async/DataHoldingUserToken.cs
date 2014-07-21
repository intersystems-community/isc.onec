using System.Net.Sockets;
using isc.onec.bridge;
using NLog;

namespace isc.onec.tcp.async {
	internal sealed class DataHoldingUserToken {
		internal readonly Mediator Mediator;

		internal DataHolder DataHolder;

		internal readonly int BufferOffsetReceive;
		internal readonly int PermanentReceiveMessageOffset;
		internal readonly int BufferOffsetSend;

		private readonly int tokenId; // for testing only		

		internal int LengthOfCurrentIncomingMessage;

		// receiveMessageOffset is used to mark the byte position where the message
		// begins in the receive buffer. This value can sometimes be out of
		// bounds for the data stream just received. But, if it is out of bounds, the 
		// code will not access it.
		internal int ReceiveMessageOffset;
		internal byte[] ByteArrayForPrefix;
		internal readonly int ReceivePrefixLength;
		internal int ReceivedPrefixBytesDoneCount;
		internal int ReceivedMessageBytesDoneCount;

		// This variable will be needed to calculate the value of the
		// receiveMessageOffset variable in one situation. Notice that the
		// name is similar but the usage is different from the variable
		// receiveSendToken.receivePrefixBytesDone.
		internal int RecPrefixBytesDoneThisOp;

		internal int SendBytesRemainingCount;
		internal readonly int SendPrefixLength;
		internal byte[] DataToSend;
		internal int BytesSentAlreadyCount;

		private Server server;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		internal DataHoldingUserToken(SocketAsyncEventArgs sendReceiveArgs,
				SocketListenerSettings socketListenerSettings,
				int tokenId) {
			this.Mediator = new Mediator(sendReceiveArgs);
			this.BufferOffsetReceive = sendReceiveArgs.Offset;
			this.BufferOffsetSend = sendReceiveArgs.Offset + socketListenerSettings.BufferSize;
			this.ReceivePrefixLength = socketListenerSettings.ReceivePrefixLength;
			this.SendPrefixLength = socketListenerSettings.SendPrefixLength;
			this.ReceiveMessageOffset = sendReceiveArgs.Offset + socketListenerSettings.ReceivePrefixLength;
			this.PermanentReceiveMessageOffset = sendReceiveArgs.Offset + socketListenerSettings.ReceivePrefixLength;
			this.tokenId = tokenId;
		}

		~DataHoldingUserToken() {
			Logger.Debug("DataHoldingUserToken destructor is called");
			this.server = null;
		}

		// Let's use an ID for this object during testing, just so we can see what
		// is happening better if we want to.
		internal int TokenId {
			get {
				return this.tokenId;
			}
		}

		internal void CreateNewDataHolder() {
			this.DataHolder = new DataHolder();
		}

		public void Reset() {
			this.ReceivedPrefixBytesDoneCount = 0;
			this.ReceivedMessageBytesDoneCount = 0;
			this.RecPrefixBytesDoneThisOp = 0;
			this.ReceiveMessageOffset = this.PermanentReceiveMessageOffset;
		}

		public void CleanUp() {
			Logger.Debug("Cleanup is called");
			if (this.server != null) {
				this.server.Disconnect();
				this.server = null;
			}
		}

		internal Server Server {
			get {
				return this.server;
			}
		}

		internal void StartSession() {
			Logger.Debug("Creating new isc.onec.bridge.Server");
			this.server = new Server();
		}
	}
}
