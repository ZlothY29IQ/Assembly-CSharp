using System;
using System.Collections.Generic;
using UnityEngine;

public class GRAttributes : MonoBehaviour
{
	[Serializable]
	public struct GRAttributePair
	{
		public GRAttributeType type;

		public float value;
	}

	[SerializeField]
	private List<GRAttributePair> startingAttributes;

	[NonSerialized]
	private GRBonusSystem bonusSystem = new GRBonusSystem();

	public Dictionary<GRAttributeType, int> defaultAttributes = new Dictionary<GRAttributeType, int>();

	private void Awake()
	{
		foreach (GRAttributePair startingAttribute in startingAttributes)
		{
			defaultAttributes[startingAttribute.type] = (int)(startingAttribute.value * 100f);
		}
		bonusSystem.Init(this);
	}

	public bool HasBeenInitialized()
	{
		return bonusSystem.GetDefaultAttributes() != null;
	}

	public void AddAttribute(GRAttributeType type, float value)
	{
		defaultAttributes[type] = (int)(value * 100f);
	}

	public void AddBonus(GRBonusEntry entry)
	{
		bonusSystem.AddBonus(entry);
	}

	public void RemoveBonus(GRBonusEntry entry)
	{
		bonusSystem.RemoveBonus(entry);
	}

	public float CalculateFinalFloatValueForAttribute(GRAttributeType attributeType)
	{
		int num = bonusSystem.CalculateFinalValueForAttribute(attributeType);
		float result = 0f;
		if (num > 0)
		{
			result = (float)num / 100f;
		}
		return result;
	}

	public int CalculateFinalValueForAttribute(GRAttributeType attributeType)
	{
		int num = bonusSystem.CalculateFinalValueForAttribute(attributeType);
		if (num > 0)
		{
			num /= 100;
		}
		return num;
	}

	public bool HasValueForAttribute(GRAttributeType attributeType)
	{
		return bonusSystem.HasValueForAttribute(attributeType);
	}
}
