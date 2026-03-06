using System.Collections.Generic;

public static class SIResourceHelper
{
	public static bool IsInOrder(this IList<SIResource.ResourceCost> cost)
	{
		if (cost == null)
		{
			return true;
		}
		SIResource.ResourceType resourceType = (SIResource.ResourceType)(-1);
		foreach (SIResource.ResourceCost item in cost)
		{
			if (item.type <= resourceType)
			{
				return false;
			}
			resourceType = item.type;
		}
		return true;
	}

	public static bool IsValid(this IList<SIResource.ResourceCost> cost)
	{
		if (cost == null || cost.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (SIResource.ResourceCost item in cost)
		{
			int num2 = 1 << (int)item.type;
			if ((num & num2) != 0)
			{
				return false;
			}
			if (item.amount <= 0)
			{
				return false;
			}
			num |= num2;
		}
		return true;
	}

	public static bool IsValid_AllowZero(this IList<SIResource.ResourceCost> cost)
	{
		if (cost == null || cost.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (SIResource.ResourceCost item in cost)
		{
			int num2 = 1 << (int)item.type;
			if ((num & num2) != 0)
			{
				return false;
			}
			if (item.amount < 0)
			{
				return false;
			}
			num |= num2;
		}
		return true;
	}

	public static SIResource.ResourceCategoryCost GetCategoryCosts(this IList<SIResource.ResourceCost> costs)
	{
		int num = 0;
		int num2 = 0;
		if (costs != null)
		{
			foreach (SIResource.ResourceCost cost in costs)
			{
				if (cost.type == SIResource.ResourceType.TechPoint)
				{
					num += cost.amount;
				}
				else
				{
					num2 += cost.amount;
				}
			}
		}
		return new SIResource.ResourceCategoryCost(num, num2);
	}

	public static List<SIResource.ResourceCost> GetTotalResourceCost(this IList<SIResource.ResourceCost> baseCost, IList<SIResource.ResourceCost> additiveCosts)
	{
		List<SIResource.ResourceCost> list = new List<SIResource.ResourceCost>(baseCost);
		foreach (SIResource.ResourceCost additiveCost in additiveCosts)
		{
			list.Add(additiveCost);
		}
		return list;
	}

	public static List<SIResource.ResourceCost> GetMax(this IList<SIResource.ResourceCost> baseCost, IList<SIResource.ResourceCost> additiveCosts)
	{
		List<SIResource.ResourceCost> list = new List<SIResource.ResourceCost>(baseCost);
		foreach (SIResource.ResourceCost additiveCost in additiveCosts)
		{
			list.Add(additiveCost);
		}
		list.Sort();
		return list;
	}

	public static int GetAmount(this IList<SIResource.ResourceCost> costs, SIResource.ResourceType resourceType)
	{
		foreach (SIResource.ResourceCost cost in costs)
		{
			if (cost.type == resourceType)
			{
				return cost.amount;
			}
		}
		return 0;
	}

	public static void SetAmount(this List<SIResource.ResourceCost> costs, SIResource.ResourceType resourceType, int amount)
	{
		for (int i = 0; i < costs.Count; i++)
		{
			SIResource.ResourceCost value = costs[i];
			if (value.type == resourceType)
			{
				value.amount = amount;
				costs[i] = value;
				return;
			}
		}
		costs.Add(new SIResource.ResourceCost(resourceType, amount));
	}

	public static void AddResourceCost(this List<SIResource.ResourceCost> baseCost, SIResource.ResourceCost additiveCost)
	{
		for (int i = 0; i < baseCost.Count; i++)
		{
			SIResource.ResourceCost value = baseCost[i];
			if (value.type == additiveCost.type)
			{
				value.amount += additiveCost.amount;
				baseCost[i] = value;
				return;
			}
		}
		baseCost.Add(additiveCost);
	}

	public static void AddResourceCost(this List<SIResource.ResourceCost> baseCost, IList<SIResource.ResourceCost> additiveCost)
	{
		foreach (SIResource.ResourceCost item in additiveCost)
		{
			baseCost.AddResourceCost(item);
		}
	}

	public static int GetTechPointCost(this IList<SIResource.ResourceCost> costs)
	{
		int num = 0;
		foreach (SIResource.ResourceCost cost in costs)
		{
			if (cost.type == SIResource.ResourceType.TechPoint)
			{
				num += cost.amount;
			}
		}
		return num;
	}

	public static int GetMiscCost(this IList<SIResource.ResourceCost> costs)
	{
		int num = 0;
		foreach (SIResource.ResourceCost cost in costs)
		{
			if (cost.type != SIResource.ResourceType.TechPoint)
			{
				num += cost.amount;
			}
		}
		return num;
	}

	public static void SetResourceCost(this IList<SIResource.ResourceCost> costs, SIResource.ResourceCategoryCost desiredCosts)
	{
		costs.SetTechPointCost(desiredCosts.techPoints);
		costs.SetMiscCost(desiredCosts.misc);
	}

	public static void AddResourceCost(this IList<SIResource.ResourceCost> baseCost, SIResource.ResourceCategoryCost additiveCost)
	{
		baseCost.SetTechPointCost(baseCost.GetTechPointCost() + additiveCost.techPoints);
		baseCost.SetMiscCost(baseCost.GetMiscCost() + additiveCost.misc);
	}

	public static void SetTechPointCost(this IList<SIResource.ResourceCost> baseCost, int desiredCost)
	{
		for (int i = 0; i < baseCost.Count; i++)
		{
			SIResource.ResourceCost value = baseCost[i];
			if (value.type == SIResource.ResourceType.TechPoint)
			{
				value.amount = desiredCost;
				baseCost[i] = value;
				return;
			}
		}
		baseCost.Add(new SIResource.ResourceCost(SIResource.ResourceType.TechPoint, desiredCost));
	}

	public static void SetMiscCost(this IList<SIResource.ResourceCost> baseCost, int desiredCost)
	{
		int miscCost = baseCost.GetMiscCost();
		if (miscCost == desiredCost)
		{
			return;
		}
		for (int i = 0; i < baseCost.Count; i++)
		{
			SIResource.ResourceCost value = baseCost[i];
			if (value.type != SIResource.ResourceType.TechPoint)
			{
				value.amount += desiredCost - miscCost;
				if (value.amount >= 1)
				{
					baseCost[i] = value;
					return;
				}
				baseCost.RemoveAt(i--);
				miscCost = baseCost.GetMiscCost();
				if (miscCost == desiredCost)
				{
					return;
				}
			}
		}
		if (desiredCost != miscCost)
		{
			baseCost.Add(new SIResource.ResourceCost(SIResource.ResourceType.StrangeWood, desiredCost - miscCost));
		}
	}
}
