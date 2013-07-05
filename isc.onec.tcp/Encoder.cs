using System;
using System.Collections.Generic;

namespace isc.onec.tcp
{
	public class MessageEncoder
	{
		private string[] reply;
		public MessageEncoder(string[] reply)
		{
			this.reply = reply;
		  
		}
		public byte[] encode()
		{
			byte[] type,value,length;
			type = BitConverter.GetBytes(Convert.ToSByte(reply[0]));

			if (reply[1] != null)
			{
				value = System.Text.Encoding.UTF8.GetBytes(reply[1]);
				length = BitConverter.GetBytes(Convert.ToUInt16(value.Length));
			}
			else
			{
				length = BitConverter.GetBytes(Convert.ToUInt16(0));
				value = new byte[0];
			}
			
			return combine(type,length,value);
		}
		public static byte[] combine(byte[] first, byte[] second, byte[] third)
		{
			byte[] ret = new byte[first.Length + second.Length + third.Length];
			Buffer.BlockCopy(first, 0, ret, 0, first.Length);
			Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
			Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
							 third.Length);
			return ret;
		}
	}
}
