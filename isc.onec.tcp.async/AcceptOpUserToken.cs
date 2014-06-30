using System;
using System.Net.Sockets;
using System.Text;

namespace isc.onec.tcp.async
{
	internal sealed class AcceptOpUserToken {
		//The only reason to use this UserToken in our app is to give it an tokenId,
		//so that you can see it in the program flow. Otherwise, you would not need it.

		
		private Int32 tokenId; //for testing only

		public AcceptOpUserToken(Int32 tokenId) {
			this.tokenId = tokenId;
		}

		internal Int32 TokenId {
			get {
				return this.tokenId;
			}
			set {
				this.tokenId = value;
			}
		}
	}
}
