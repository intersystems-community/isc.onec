using System;
using System.Diagnostics;
using System.Net.Sockets;
using isc.general;
using NLog;

namespace isc.onec.tcp.async {
	internal static class MessageHandler {
		private static readonly EventLog EventLog = EventLogFactory.Instance;

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="receiveSendToken"></param>
		/// <param name="remainingBytesToProcess"></param>
		/// <returns>whether incoming TCP message is ready</returns>
		internal static bool HandleMessage(byte[] source,
				DataHoldingUserToken receiveSendToken,
				int remainingBytesToProcess) {
			// Create the array where we'll store the complete message, 
			// if it has not been created on a previous receive op.
			if (receiveSendToken.ReceivedMessageBytesDoneCount == 0) {
				if (receiveSendToken.LengthOfCurrentIncomingMessage > 500000) {
					throw new Exception("receiveSendToken.lengthOfCurrentIncomingMessage:" + receiveSendToken.LengthOfCurrentIncomingMessage);
				}
				receiveSendToken.DataHolder.DataMessageReceived = new byte[receiveSendToken.LengthOfCurrentIncomingMessage];
			}

			// Remember there is a receiveSendToken.receivedPrefixBytesDoneCount
			// variable, which allowed us to handle the prefix even when it
			// requires multiple receive ops. In the same way, we have a 
			// receiveSendToken.receivedMessageBytesDoneCount variable, which
			// helps us handle message data, whether it requires one receive
			// operation or many.
			try {
				Buffer.BlockCopy(source,
						receiveSendToken.ReceiveMessageOffset,
						receiveSendToken.DataHolder.DataMessageReceived,
						receiveSendToken.ReceivedMessageBytesDoneCount,
						remainingBytesToProcess);
			} catch (ArgumentException ae) {
				EventLog.WriteEntry(ae.ToStringWithIlOffsets(), EventLogEntryType.Error);
				EventLog.WriteEntry("Exception context: client: " + receiveSendToken.Server.Client
					+ "; source: " + source.Length
					+ " byte(s); source offset : " + receiveSendToken.ReceiveMessageOffset
					+ "; destination: " + receiveSendToken.DataHolder.DataMessageReceived.Length
					+ " byte(s); destination offset: " + receiveSendToken.ReceivedMessageBytesDoneCount
					+ "; count : " + remainingBytesToProcess
					+ "; full message length: " + receiveSendToken.LengthOfCurrentIncomingMessage, EventLogEntryType.Error);

				throw;
			}

			if (remainingBytesToProcess + receiveSendToken.ReceivedMessageBytesDoneCount == receiveSendToken.LengthOfCurrentIncomingMessage) {
				// If we are inside this if-statement, then we got 
				// the end of the message. In other words,
				// the total number of bytes we received for this message matched the 
				// message length value that we got from the prefix.
				// Write/append the bytes received to the byte array in the 
				// DataHolder object that we are using to store our data.
				return true;
			} else {
				// If we are inside this else-statement, then that means that we
				// need another receive op. We still haven't got the whole message,
				// even though we have examined all the data that was received.
				// Not a problem. In SocketListener.ProcessReceive we will just call
				// StartReceive to do another receive op to receive more data.
				receiveSendToken.ReceiveMessageOffset -= receiveSendToken.RecPrefixBytesDoneThisOp;
				receiveSendToken.ReceivedMessageBytesDoneCount += remainingBytesToProcess;
				return false;
			}
		}
	}
}
