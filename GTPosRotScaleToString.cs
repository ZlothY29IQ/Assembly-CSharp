using System.Text.RegularExpressions;
using UnityEngine;

public static class GTPosRotScaleToString
{
	public const string k_LocalPRSLabel = "LocalPRS";

	public const string k_WorldPRSLabel = "WorldPRS";

	public static string ToString(Vector3 pos, Vector3 rot, Vector3 scale, bool isWorldSpace, string parentPath = null)
	{
		string text = (isWorldSpace ? "WorldPRS" : "LocalPRS");
		string text2 = text + " { p=" + ValToStr(pos) + ", r=" + ValToStr(rot) + ", s=" + ValToStr(scale);
		if (!string.IsNullOrEmpty(parentPath))
		{
			text2 = text2 + " parent=\"" + parentPath + "\"";
		}
		return text2 + " }";
	}

	private static string ValToStr(Vector3 v)
	{
		return $"({v.x:R}, {v.y:R}, {v.z:R})";
	}

	public static bool ParseIsWorldSpace(string input)
	{
		return input.Contains("WorldPRS");
	}

	public static string ParseParentPath(string input)
	{
		MatchCollection matchCollection = Regex.Matches(input, "parent\\s*=\\s*\"(?<parent>.*?)\"");
		if (matchCollection.Count <= 0)
		{
			return null;
		}
		return matchCollection[0].Groups["parent"].Value;
	}

	public static bool TryParsePos(string input, out Vector3 v)
	{
		return TryParseVec3_internal(GTRegex.k_Pos, input, out v);
	}

	public static bool TryParseRot(string input, out Vector3 v)
	{
		return TryParseVec3_internal(GTRegex.k_Rot, input, out v);
	}

	public static bool TryParseScale(string input, out Vector3 v)
	{
		if (!TryParseVec3_internal(GTRegex.k_Scale, input, out v))
		{
			return TryParseVec3_internal(GTRegex.k_Vec3, input, out v);
		}
		return true;
	}

	public static bool TryParseVec3(string input, out Vector3 v)
	{
		return TryParseVec3_internal(GTRegex.k_Vec3, input, out v);
	}

	private static bool TryParseVec3_internal(Regex regex, string input, out Vector3 v)
	{
		v = Vector3.zero;
		MatchCollection matchCollection = regex.Matches(input);
		if (matchCollection.Count <= 0)
		{
			return false;
		}
		v = StringToVector3(matchCollection[0]);
		return true;
	}

	private static Vector3 StringToVector3(Match match)
	{
		float x = float.Parse(match.Groups["x"].Value);
		float y = float.Parse(match.Groups["y"].Value);
		float z = float.Parse(match.Groups["z"].Value);
		return new Vector3(x, y, z);
	}
}
