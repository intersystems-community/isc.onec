using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;


namespace isc.onec.tcp.async {
	internal sealed class Mediator {
		private readonly IncomingDataPreparer incomingDataPreparer;
		private readonly OutgoingDataPreparer outgoingDataPreparer;
		private DataHolder theDataHolder;
		private readonly SocketAsyncEventArgs socketAsyncEventArgs;

		/// <summary>
		/// Called by <see cref="isc.onec.tcp.async.DataHoldingUserToken"/>.
		/// </summary>
		/// <param name="socketAsyncEventArgs"></param>
		internal Mediator(SocketAsyncEventArgs socketAsyncEventArgs)
		{
			
			this.socketAsyncEventArgs = socketAsyncEventArgs;
			this.incomingDataPreparer = new IncomingDataPreparer(this.socketAsyncEventArgs);
			this.outgoingDataPreparer = new OutgoingDataPreparer();			
		}

		internal void HandleData(DataHolder incomingDataHolder) {
			this.theDataHolder = this.incomingDataPreparer.HandleReceivedData(incomingDataHolder, this.socketAsyncEventArgs);
		}
		
		internal void PrepareOutgoingData() {
			this.outgoingDataPreparer.PrepareOutgoingData(this.socketAsyncEventArgs, this.theDataHolder);			
		}

		internal SocketAsyncEventArgs SocketAsyncEventArgs {
			get {
				return this.socketAsyncEventArgs;
			}
		}
	}
}
