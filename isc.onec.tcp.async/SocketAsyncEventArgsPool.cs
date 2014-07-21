using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace isc.onec.tcp.async
{	
	internal sealed class SocketAsyncEventArgsPool {		
		// just for assigning an ID so we can watch our objects while testing.
		private int nextTokenId;
		
		// Pool of reusable SocketAsyncEventArgs objects.
		// WTF? Consider using a concurrent collection *instead* of ths entire class.
		private readonly Stack<SocketAsyncEventArgs> pool;
		
		// initializes the object pool to the specified size.
		// "capacity" = Maximum number of SocketAsyncEventArgs objects
		internal SocketAsyncEventArgsPool(int capacity)
		{
			this.pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		// The number of SocketAsyncEventArgs instances in the pool.
		// WTF? This may fail to return the most current value. 
		internal int Count
		{
			get { return this.pool.Count; }
		}

		internal int NextTokenId {
			get {
				return Interlocked.Increment(ref this.nextTokenId);
			}
		}

		// Removes a SocketAsyncEventArgs instance from the pool.
		// returns SocketAsyncEventArgs removed from the pool.
		internal SocketAsyncEventArgs Pop()
		{
			lock (this.pool)
			{
				return this.pool.Pop();
			}
		}

		// Add a SocketAsyncEventArg instance to the pool. 
		// "item" = SocketAsyncEventArgs instance to add to the pool.
		internal void Push(SocketAsyncEventArgs item)
		{
			if (item == null) 
			{ 
				throw new ArgumentNullException("item"); 
			}
			lock (this.pool)
			{
				this.pool.Push(item);
			}
		}
	}
}
