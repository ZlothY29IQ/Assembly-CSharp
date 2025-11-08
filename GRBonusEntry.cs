using System;
using UnityEngine;

[Serializable]
public class GRBonusEntry
{
	public enum GRBonusType
	{
		None,
		Additive,
		Multiplicative
	}

	private static int idCounter;

	public GRBonusType bonusType;

	public GRAttributeType attributeType;

	[SerializeField]
	private float bonusValue;

	public Func<int, GRBonusEntry, int> customBonus;

	public int id { get; private set; }

	private GRBonusEntry()
	{
		idCounter++;
		id = idCounter;
	}

	public int GetBonusValue()
	{
		return (int)(bonusValue * 100f);
	}

	public override string ToString()
	{
		bool flag = customBonus != null;
		return $"GRBonusEntry BonusType {bonusType} AttributeType {attributeType} BonusValue {bonusValue} Id {id} CustomBonusSet {flag}";
	}
}
