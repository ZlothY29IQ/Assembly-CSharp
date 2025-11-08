using System;

namespace Utilities;

public static class PathUtils
{
	private static readonly char[] kPathSeps = new char[2] { '\\', '/' };

	private const string kFwdSlash = "/";

	public static string Resolve(params string[] subPaths)
	{
		if (subPaths == null || subPaths.Length == 0)
		{
			return null;
		}
		string[] value = string.Concat(subPaths).Split(kPathSeps, StringSplitOptions.RemoveEmptyEntries);
		return Uri.UnescapeDataString(new Uri(string.Join("/", value)).AbsolutePath);
	}
}
