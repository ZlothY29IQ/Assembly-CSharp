using System;
using System.Diagnostics;

namespace BuildSafe;

public static class Callbacks
{
	[Conditional("UNITY_EDITOR")]
	public class DidReloadScripts : Attribute
	{
		public bool activeOnly;

		public DidReloadScripts(bool activeOnly = false)
		{
			this.activeOnly = activeOnly;
		}
	}
}
