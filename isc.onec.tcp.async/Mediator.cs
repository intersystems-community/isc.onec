using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace isc.onec.tcp.async {
	internal sealed class Mediator {
		private readonly OutgoingDataPreparer outgoingDataPreparer;
		private DataHolder dataHolder;
		private readonly SocketAsyncEventArgs socketAsyncEventArgs;

		/// <summary>
		/// Called by <see cref="isc.onec.tcp.async.DataHoldingUserToken"/>.
		/// </summary>
		/// <param name="socketAsyncEventArgs"></param>
		internal Mediator(SocketAsyncEventArgs socketAsyncEventArgs) {
			this.socketAsyncEventArgs = socketAsyncEventArgs;
			this.outgoingDataPreparer = new OutgoingDataPreparer();			
		}

		internal void HandleData(DataHolder dataHolder) {
			this.dataHolder = dataHolder;
		}
		
		internal void PrepareOutgoingData() {
			this.outgoingDataPreparer.PrepareOutgoingData(this.socketAsyncEventArgs, this.dataHolder);			
		}

		internal SocketAsyncEventArgs SocketAsyncEventArgs {
			get {
				return this.socketAsyncEventArgs;
			}
		}
	}
}
