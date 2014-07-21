using System;
using System.Diagnostics;
using System.Net.Sockets;
using isc.general;
using isc.onec.bridge;
using NLog;

namespace isc.onec.tcp.async {
	internal sealed class OutgoingDataPreparer {
		private DataHolder theDataHolder;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly EventLog EventLog = EventLogFactory.Instance;

		public OutgoingDataPreparer()
		{			
		}

		internal void PrepareOutgoingData(SocketAsyncEventArgs e, DataHolder handledDataHolder)
		{
			DataHoldingUserToken theUserToken = (DataHoldingUserToken)e.UserToken;

			this.theDataHolder = handledDataHolder;

			// In this example code, we will send back the receivedTransMissionId,
			// followed by the
			// message that the client sent to the server. And we must
			// prefix it with the length of the message. So we put 3 
			// things into the array.
			// 1) prefix,
			// 2) receivedTransMissionId,
			// 3) the message that we received from the client, which
			// we stored in our DataHolder until we needed it.
			// That is our communication protocol. The client must know the protocol.

			// Convert the receivedTransMissionId to byte array.
////			Byte[] idByteArray = BitConverter.GetBytes(theDataHolder.receivedTransMissionId);

			// Determine the length of all the data that we will send back.
////			Int32 lengthOfCurrentOutgoingMessage = idByteArray.Length + theDataHolder.dataMessageReceived.Length;
			byte[] reply;
			if (this.theDataHolder.IsError)
			{
				reply = SendError(theUserToken.Server);
			}
			else
			{
				reply = process(theUserToken.Server, this.theDataHolder.DataMessageReceived);
			}
			int lengthOfCurrentOutgoingMessage = reply.Length;

			// So, now we convert the length integer into a byte array.
			byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(lengthOfCurrentOutgoingMessage);
			
			// Create the byte array to send.
			theUserToken.DataToSend = new byte[theUserToken.SendPrefixLength + lengthOfCurrentOutgoingMessage];
			
			// Now copy the 3 things to the theUserToken.dataToSend.
			Buffer.BlockCopy(arrayOfBytesInPrefix, 0, theUserToken.DataToSend, 0, theUserToken.SendPrefixLength);

			// The message that the client sent is already in a byte array, in DataHolder.
			Buffer.BlockCopy(reply, 0, theUserToken.DataToSend, theUserToken.SendPrefixLength, reply.Length);
			
			theUserToken.SendBytesRemainingCount = theUserToken.SendPrefixLength + lengthOfCurrentOutgoingMessage;
			theUserToken.BytesSentAlreadyCount = 0;
		}

		private static byte[] SendError(Server server) {
			string message;
			try {
				/*
				 * Return value ignored.
				 */
				server.Run(RequestMessage.Disconnect);
				Logger.Error("OutgoingDataPreparer.sendError(): error was sent");
				message = "Bridge: Fatal network error";
			} catch (Exception e) {
				Logger.ErrorException("Unprocessed exception:", e);
				message = "Bridge: Fatal network error. Unprocessed exception.";
			}

			string[] reply = Response.NewException(message).Serialize();
			return new MessageEncoder(reply).encode();
		}

		private static byte[] process(Server server, byte[] data) {
			string[] reply;
			try {
				RequestMessage request = new RequestMessage(data);

				if (server == null) {
					/*
					 * Most probably, this doesn't ever happen,
					 * and the NullReferenceException observed is https://bitbucket.org/bass/isc.onec/issue/4
					 */
					const string Message = "OutgoingDataPreparer.process(): no server object.";
					Logger.Error(Message);
					EventLog.WriteEntry(Message, EventLogEntryType.Error);
					reply = Response.NewException(Message).Serialize();
				} else {
					reply = server.Run(request).Serialize();
				}
			} catch (Exception e) {
				var message = e.ToStringWithIlOffsets();
				EventLog.WriteEntry(message, EventLogEntryType.Error);
				reply = Response.NewException(message).Serialize();
			}
			return new MessageEncoder(reply).encode();
		}
	}
}
