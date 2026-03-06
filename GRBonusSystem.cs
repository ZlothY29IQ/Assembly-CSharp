using System.Collections.Generic;
using UnityEngine;

public class GRBonusSystem
{
	private GRAttributes defaultAttributes;

	private Dictionary<GRAttributeType, List<GRBonusEntry>> currentAdditiveBonuses = new Dictionary<GRAttributeType, List<GRBonusEntry>>();

	private Dictionary<GRAttributeType, List<GRBonusEntry>> currentMultiplicativeBonuses = new Dictionary<GRAttributeType, List<GRBonusEntry>>();

	public void Init(GRAttributes attributes)
	{
		defaultAttributes = attributes;
	}

	public GRAttributes GetDefaultAttributes()
	{
		return defaultAttributes;
	}

	public void AddBonus(GRBonusEntry entry)
	{
		if (entry.bonusType != GRBonusEntry.GRBonusType.None)
		{
			if (!currentAdditiveBonuses.ContainsKey(entry.attributeType))
			{
				currentAdditiveBonuses[entry.attributeType] = new List<GRBonusEntry>();
			}
			if (!currentMultiplicativeBonuses.ContainsKey(entry.attributeType))
			{
				currentMultiplicativeBonuses[entry.attributeType] = new List<GRBonusEntry>();
			}
			if (entry.bonusType == GRBonusEntry.GRBonusType.Additive)
			{
				currentAdditiveBonuses[entry.attributeType].Add(entry);
			}
			else if (entry.bonusType == GRBonusEntry.GRBonusType.Multiplicative)
			{
				currentMultiplicativeBonuses[entry.attributeType].Add(entry);
			}
		}
	}

	public void RemoveBonus(GRBonusEntry entry)
	{
		foreach (List<GRBonusEntry> value in currentAdditiveBonuses.Values)
		{
			value.Remove(entry);
		}
		foreach (List<GRBonusEntry> value2 in currentMultiplicativeBonuses.Values)
		{
			value2.Remove(entry);
		}
	}

	public bool HasValueForAttribute(GRAttributeType attributeType)
	{
		if (defaultAttributes != null)
		{
			return defaultAttributes.defaultAttributes.ContainsKey(attributeType);
		}
		return false;
	}

	public int CalculateFinalValueForAttribute(GRAttributeType attributeType)
	{
		if (defaultAttributes == null)
		{
			Debug.LogErrorFormat("CalculateFinalValueForAttribute DefaultAttributes null.  Please fix configuration.");
			return 0;
		}
		if (!defaultAttributes.defaultAttributes.ContainsKey(attributeType))
		{
			Debug.LogErrorFormat("CalculateFinalValueForAttribute DefaultAttributes Does not have entry for {0}.  Please fix configuration.", attributeType);
			return 0;
		}
		int num = defaultAttributes.defaultAttributes[attributeType];
		if (currentAdditiveBonuses.ContainsKey(attributeType))
		{
			foreach (GRBonusEntry item in currentAdditiveBonuses[attributeType])
			{
				num = ((item.customBonus == null) ? (num + item.GetBonusValue()) : item.customBonus(num, item));
			}
		}
		if (currentMultiplicativeBonuses.ContainsKey(attributeType))
		{
			foreach (GRBonusEntry item2 in currentMultiplicativeBonuses[attributeType])
			{
				if (item2.customBonus != null)
				{
					num = item2.customBonus(num, item2);
					continue;
				}
				float num2 = (float)item2.GetBonusValue() / 100f;
				num = (int)((float)num * num2);
			}
		}
		return num;
	}
}
