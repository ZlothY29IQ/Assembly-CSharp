public static class StringExts
{
	private static char[] _escapeChars;

	public static string EscapeCsv(this string field)
	{
		if (_escapeChars == null)
		{
			_escapeChars = new char[4] { ',', '"', '\n', '\r' };
		}
		if (field.IndexOfAny(_escapeChars) != -1)
		{
			return field.Replace("\"", "\"\"");
		}
		return field;
	}
}
