using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BuilderPieceSet01", menuName = "Gorilla Tag/Builder/PieceSet", order = 0)]
public class BuilderPieceSet : ScriptableObject
{
	public enum BuilderPieceCategory
	{
		FLAT = 0,
		TALL = 1,
		HALF_HEIGHT = 2,
		BEAM = 3,
		SLOPE = 4,
		OVERSIZED = 5,
		SPECIAL_DISPLAY = 6,
		FUNCTIONAL = 18,
		DECORATIVE = 19,
		MISC = 20
	}

	[Serializable]
	public class BuilderPieceSubset
	{
		[Tooltip("(Optional) Text to put on the shelf button if not the set name")]
		public string shelfButtonName;

		public LocalizedString localizedShelfButtonName;

		public BuilderPieceCategory pieceCategory;

		public List<PieceInfo> pieceInfos;

		public string GetShelfButtonName()
		{
			return shelfButtonName;
		}
	}

	[Serializable]
	public struct PieceInfo
	{
		public BuilderPiece piecePrefab;

		[Tooltip("(Optional) should this piece use a materialID other than the set's materialID")]
		public bool overrideSetMaterial;

		[Tooltip("material type string should match an entry in this prefab's BuilderMaterialOptions\nIf multiple are in the list the piece will cycle through materials when spawned\nTo have each variant on the shelf create a new pieceInfo for each color")]
		public string[] pieceMaterialTypes;
	}

	public class BuilderDisplayGroup
	{
		public string displayName;

		public List<BuilderPieceSubset> pieceSubsets;

		public string defaultMaterial;

		public int setID;

		public string uniqueGroupID;

		public BuilderDisplayGroup()
		{
			displayName = string.Empty;
			pieceSubsets = new List<BuilderPieceSubset>();
			defaultMaterial = string.Empty;
			setID = -1;
			uniqueGroupID = string.Empty;
		}

		public BuilderDisplayGroup(string groupName, string material, int inSetID, string groupID)
		{
			displayName = groupName;
			pieceSubsets = new List<BuilderPieceSubset>();
			defaultMaterial = material;
			setID = inSetID;
			uniqueGroupID = groupID;
		}

		public int GetDisplayGroupIdentifier()
		{
			return uniqueGroupID.GetStaticHash();
		}
	}

	[Tooltip("Display Name - Fallback for Localization")]
	public string setName;

	public GameObject displayModel;

	[Tooltip("If this should error if no localization is found")]
	public bool isLocalized;

	[Tooltip("Localized Display Name")]
	public LocalizedString setLocName;

	[FormerlySerializedAs("uniqueId")]
	[Tooltip("If purchaseable, this should be a valid playfabID starting with LD\nIf a starter set, this just needs to be a unique string from the other set IDs")]
	public string playfabID;

	[Tooltip("(Optional) Default Material ID applied to all prefabs with BuilderMaterialOptions")]
	public string materialId;

	[Tooltip("(Optional) If this set is not available on launch day use scheduling")]
	public bool isScheduled;

	public string scheduledDate = "1/1/0001 00:00:00";

	[Tooltip("A group of pieces on the same shelf")]
	public List<BuilderPieceSubset> subsets;

	public string SetName => setName;

	public int GetIntIdentifier()
	{
		return playfabID.GetStaticHash();
	}

	public DateTime GetScheduleDateTime()
	{
		if (isScheduled)
		{
			try
			{
				return DateTime.Parse(scheduledDate, CultureInfo.InvariantCulture);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}
		return DateTime.MinValue;
	}
}
