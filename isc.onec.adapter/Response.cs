using System;
using isc.general;

namespace isc.onec.bridge
{
	public class Response
	{
		public enum Type { VOID=1, DATA=2, OBJECT=3, EXCEPTION=4 };
		public Type type;
		public object value;

		public Response()
		{
			this.type = Type.VOID;
			this.value = "";
		}
		public Response(Exception e)
		{
			this.type = Type.EXCEPTION;
			this.value = e.ToStringWithIlOffsets();
		}
		public Response(bool value)
		{
			this.type = Type.DATA;
			if (value) this.value = "1";
			else this.value = "0";
		}
		public Response(Type type, object value)
		{
			this.type = type;
			this.value = value;
		}
	}
}
