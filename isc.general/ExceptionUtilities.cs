using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace isc.general {
	public static class ExceptionUtilities {
		public static void WriteToConsole(this Exception e) {
			GetStackTrace(e).ForEach(Console.WriteLine);
		}

		public static string ToStringWithIlOffsets(this Exception e) {
			return GetStackTrace(e).Aggregate((string) null, (left, right) => left == null || left.Length == 0 ? right : left + "\n" + right);
		}

		/// <summary>
		/// Returns the stack trace of an exception with IL offsets.
		/// </summary>
		/// <param name="e"></param>
		/// <returns>the stack trace of an exception with IL offsets</returns>
		public static List<string> GetStackTrace(this Exception e) {
			List<string> lines = new List<string>();
			lines.Add(e.GetType() + ": " + e.Message);

			var stackTrace = new StackTrace(e);
			stackTrace.GetFrames().ToList().ForEach(stackFrame => {
				var method = stackFrame.GetMethod();
				lines.Add("   at " + method.ReflectedType.FullName + "." + method.Name + "("
					+ method.GetParameters().Select(p => p.ParameterType.Name).Aggregate((string) null, (left, right) => left == null || left.Length == 0 ? right : left + ", " + right)
					+ ") in " + method.Module.Name + ":IL offset 0x" + stackFrame.GetILOffset().ToString("x"));
			});

			return lines;
		}
	}
}