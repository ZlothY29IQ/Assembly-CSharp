using System;
using System.Linq;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class ContinuousPropertyModeSO : ScriptableObject
{
	[Serializable]
	public struct CastData
	{
		public ContinuousProperty.Cast target;

		public ContinuousProperty.DataFlags additionalFlags;

		public string whatItSets;
	}

	public enum DescriptionStyle
	{
		Continuous,
		SingleThreshold,
		DualThreshold
	}

	public ContinuousProperty.Type type;

	public ContinuousProperty.DataFlags flags;

	public CastData[] castData;

	[Space]
	public DescriptionStyle descriptionStyle;

	[TextArea]
	public string afterSentence;

	[TextArea]
	public string replaceDescription;

	private string GetTestDescription
	{
		get
		{
			if (castData.Length == 0)
			{
				return "";
			}
			return "Sample Description: " + GetDescriptionForCast(castData[0].target);
		}
	}

	public bool IsCastValid(ContinuousProperty.Cast cast)
	{
		for (int i = 0; i < castData.Length; i++)
		{
			if (ContinuousProperty.CastMatches(castData[i].target, cast))
			{
				return true;
			}
		}
		return false;
	}

	public ContinuousProperty.Cast GetClosestCast(ContinuousProperty.Cast cast)
	{
		for (int i = 0; i < castData.Length; i++)
		{
			if (ContinuousProperty.CastMatches(castData[i].target, cast))
			{
				return castData[i].target;
			}
		}
		return ContinuousProperty.Cast.Null;
	}

	public ContinuousProperty.DataFlags GetFlagsForCast(ContinuousProperty.Cast cast)
	{
		for (int i = 0; i < castData.Length; i++)
		{
			if (castData[i].target == cast)
			{
				return castData[i].additionalFlags | flags;
			}
		}
		return flags;
	}

	public ContinuousProperty.DataFlags GetFlagsForClosestCast(ContinuousProperty.Cast cast)
	{
		for (int i = 0; i < castData.Length; i++)
		{
			if (ContinuousProperty.CastMatches(castData[i].target, cast))
			{
				return castData[i].additionalFlags | flags;
			}
		}
		return flags;
	}

	public string GetDescriptionForCast(ContinuousProperty.Cast cast)
	{
		for (int i = 0; i < castData.Length; i++)
		{
			if (!ContinuousProperty.CastMatches(castData[i].target, cast) && castData.Length != 1)
			{
				continue;
			}
			if (!replaceDescription.IsNullOrEmpty())
			{
				return replaceDescription;
			}
			switch (descriptionStyle)
			{
			case DescriptionStyle.Continuous:
				return "sets the " + castData[i].whatItSets + " on the " + castData[i].target.ToString() + " using the height of the curve at the provided time." + (" " + afterSentence).TrimEnd();
			case DescriptionStyle.SingleThreshold:
				return castData[i].whatItSets + " the " + type.ToString() + " when entering the 'true' part of the range.";
			case DescriptionStyle.DualThreshold:
			{
				string[] array = castData[i].whatItSets.Split('|');
				if (array.Length != 2)
				{
					return string.Format("Error! '{0}'s '{1}.{2}' does not have two string separated by '|'.", base.name, castData[i].target, "whatItSets");
				}
				return array[0] + " the " + castData[i].target.ToString() + " when entering the 'true' part of the range, " + array[1] + " the " + castData[i].target.ToString() + " when entering the 'false' part of the range.";
			}
			}
		}
		return "Invalid target\n\n" + ListValidCasts();
	}

	public string ListValidCasts()
	{
		return "Valid targets: " + string.Join(", ", castData.Select((CastData x) => x.target));
	}
}
