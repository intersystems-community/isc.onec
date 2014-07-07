using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace isc.onec.tcp.async
{
	internal sealed class IncomingDataPreparer {
		//object that will be used to lock the listOfDataHolders
		private object lockerForList = new object();
		private DataHolder theDataHolder;
		private SocketAsyncEventArgs theSaeaObject;

		public IncomingDataPreparer(SocketAsyncEventArgs e)
		{
			
			this.theSaeaObject = e;
		}
				
		private EndPoint GetRemoteEndpoint()
		{   
			return this.theSaeaObject.AcceptSocket.RemoteEndPoint;
		}

		internal DataHolder HandleReceivedData(DataHolder incomingDataHolder, SocketAsyncEventArgs theSaeaObject)
		{
			DataHoldingUserToken receiveToken = (DataHoldingUserToken)theSaeaObject.UserToken;
			
			theDataHolder = incomingDataHolder;
			theDataHolder.remoteEndpoint = this.GetRemoteEndpoint();
			
			
			return theDataHolder;
		}

			  
	}
}
