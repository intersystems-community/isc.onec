using System;

namespace isc.onec.bridge
{
	public class Request
	{
		public enum Type { DATA=1,OBJECT=2,CONTEXT=3,NUMBER=4 };
		public Type type;
		public object value;

		public Request(string oid)
		{
			if (oid == "")
			{
				this.type = Type.CONTEXT;
			}
			else
			{
				this.type = Type.OBJECT;
			}
			this.value=oid;
		}
		public Request(Type type, object value)
		{
			this.type = type;
			this.value = value;
		}
		
		public Request(int typeId, object value)
		{
			this.type = Request.numToEnum<Type>(typeId);
			this.value = value;
		}
		public string getOID()
		{
			return value.ToString();
		}
		public object getValue()
		{
			if (type == Type.NUMBER) return Convert.ToInt64(value);
			return value.ToString();
		}
	  
		public static T numToEnum<T>(int number)
		{
			return (T)Enum.ToObject(typeof(T), number);
		}
	}
}
