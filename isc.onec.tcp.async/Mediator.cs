using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace isc.onec.tcp.async {
	internal sealed class Mediator {
		private DataHolder dataHolder;
		private readonly SocketAsyncEventArgs socketAsyncEventArgs;

		/// <summary>
		/// Called by <see cref="isc.onec.tcp.async.DataHoldingUserToken"/>.
		/// </summary>
		/// <param name="socketAsyncEventArgs"></param>
		internal Mediator(SocketAsyncEventArgs socketAsyncEventArgs) {
			this.socketAsyncEventArgs = socketAsyncEventArgs;
		}

		internal DataHolder DataHolder {
			set {
				this.dataHolder = value;
			}
		}

		internal void PrepareOutgoingData() {
			OutgoingDataPreparer.PrepareOutgoingData(this.socketAsyncEventArgs, this.dataHolder);			
		}

		internal SocketAsyncEventArgs SocketAsyncEventArgs {
			get {
				return this.socketAsyncEventArgs;
			}
		}
	}
}
