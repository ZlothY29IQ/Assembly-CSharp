using System.Text;

namespace GorillaExtensions;

public static class StringExtensions
{
	public static string UnicodeStrikethrough(this string str)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char value in str)
		{
			stringBuilder.Append(value).Append('\u0336');
		}
		return stringBuilder.ToString();
	}
}
