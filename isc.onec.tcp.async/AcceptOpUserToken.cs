using System;
using System.Net.Sockets;
using System.Text;

namespace isc.onec.tcp.async {
	internal sealed class AcceptOpUserToken {
		// The only reason to use this UserToken in our app is to give it an identifier,
		// so that you can see it in the program flow. Otherwise, you would not need it.
		private int tokenId; // for testing only

		public AcceptOpUserToken(int tokenId) {
			this.tokenId = tokenId;
		}

		internal int TokenId {
			get {
				return this.tokenId;
			}
		}
	}
}
