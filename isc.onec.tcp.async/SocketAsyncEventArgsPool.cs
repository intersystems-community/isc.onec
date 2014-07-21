using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace isc.onec.tcp.async {	
	internal sealed class SocketAsyncEventArgsPool {		
		// Pool of reusable SocketAsyncEventArgs objects.
		// XXX: Consider using ConcurrentStack *instead* of ths entire class.
		private readonly Stack<SocketAsyncEventArgs> pool;
		
		// initializes the object pool to the specified size.
		// "capacity" = Maximum number of SocketAsyncEventArgs objects
		internal SocketAsyncEventArgsPool(int capacity) {
			this.pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		// The number of SocketAsyncEventArgs instances in the pool.
		internal int Count {
			get {
				lock (this.pool) {
					return this.pool.Count;
				}
			}
		}

		// Removes a SocketAsyncEventArgs instance from the pool.
		// returns SocketAsyncEventArgs removed from the pool.
		internal SocketAsyncEventArgs Pop() {
			lock (this.pool) {
				return this.pool.Pop();
			}
		}

		// Add a SocketAsyncEventArg instance to the pool. 
		// "item" = SocketAsyncEventArgs instance to add to the pool.
		internal void Push(SocketAsyncEventArgs item) {
			if (item == null) {
				throw new ArgumentNullException("item"); 
			}
			lock (this.pool) {
				this.pool.Push(item);
			}
		}
	}
}
