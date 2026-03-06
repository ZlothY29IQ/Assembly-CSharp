using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SIResource : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public struct ResourceCost(ResourceType type, int amount) : IComparable<ResourceCost>, IEquatable<ResourceCost>
	{
		public ResourceType type = type;

		public int amount = amount;

		public int CompareTo(ResourceCost other)
		{
			int num = type.CompareTo(other.type);
			if (num != 0)
			{
				return num;
			}
			return amount.CompareTo(other.amount);
		}

		public bool Equals(ResourceCost other)
		{
			if (type == other.type)
			{
				return amount == other.amount;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ResourceCost other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)type, amount);
		}

		public override string ToString()
		{
			return $"{type.ToString()}: {amount}";
		}
	}

	public struct ResourceCategoryCost(int techPoints, int misc) : IComparable<ResourceCategoryCost>, IEquatable<ResourceCategoryCost>
	{
		public int techPoints = techPoints;

		public int misc = misc;

		public int CompareTo(ResourceCategoryCost other)
		{
			int num = techPoints.CompareTo(other.techPoints);
			if (num != 0)
			{
				return num;
			}
			return misc.CompareTo(other.misc);
		}

		public bool Equals(ResourceCategoryCost other)
		{
			if (techPoints == other.techPoints)
			{
				return misc == other.misc;
			}
			return false;
		}

		public static bool operator ==(ResourceCategoryCost left, ResourceCategoryCost right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ResourceCategoryCost left, ResourceCategoryCost right)
		{
			return !left.Equals(right);
		}

		public static ResourceCategoryCost operator +(ResourceCategoryCost left, ResourceCategoryCost right)
		{
			return new ResourceCategoryCost(left.techPoints + right.techPoints, left.misc + right.misc);
		}

		public static ResourceCategoryCost operator -(ResourceCategoryCost left, ResourceCategoryCost right)
		{
			return new ResourceCategoryCost(left.techPoints - right.techPoints, left.misc - right.misc);
		}

		public static ResourceCategoryCost operator *(ResourceCategoryCost cost, int multiple)
		{
			return new ResourceCategoryCost(cost.techPoints * multiple, cost.misc * multiple);
		}

		public static ResourceCategoryCost operator *(int multiple, ResourceCategoryCost cost)
		{
			return new ResourceCategoryCost(cost.techPoints * multiple, cost.misc * multiple);
		}

		public static ResourceCategoryCost Max(ResourceCategoryCost left, ResourceCategoryCost right)
		{
			return new ResourceCategoryCost(Mathf.Max(left.techPoints, right.techPoints), Mathf.Max(left.misc, right.misc));
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(techPoints, misc);
		}
	}

	public enum ResourceType
	{
		TechPoint,
		StrangeWood,
		WeirdGear,
		VibratingSpring,
		BouncySand,
		FloppyMetal,
		Count
	}

	public enum LimitedDepositType
	{
		None,
		MonkeIdol,
		Count
	}

	public SIPlayer lastPlayerHeld;

	public GameEntity myGameEntity;

	public ResourceType type;

	public LimitedDepositType limitedDepositType;

	public bool localDeposited;

	public bool localEverGrabbed;

	[Tooltip("The amount of pitch offset allowed during spawn, in degrees.  With this set to 0, item will always spawn aligned with surface.")]
	public float spawnPitchVariance;

	public float sleepTime = 10f;

	private bool shouldSleep = true;

	private bool isSleeping;

	private float timeReleased;

	private Rigidbody _rb;

	private void Awake()
	{
		if (myGameEntity == null)
		{
			myGameEntity = GetComponent<GameEntity>();
		}
		if (myGameEntity == null)
		{
			Debug.LogError("missing gameentity reference! bad!", base.gameObject);
			return;
		}
		GameEntity gameEntity = myGameEntity;
		gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(SetLastGrabbed));
		_rb = GetComponent<Rigidbody>();
		myGameEntity.onEntityDestroyed += HandleOnDestroyed;
	}

	public void SliceUpdate()
	{
		if (!isSleeping && shouldSleep && !(Time.time < timeReleased + sleepTime))
		{
			_rb.isKinematic = true;
			isSleeping = true;
		}
	}

	public void SetLastGrabbed()
	{
		lastPlayerHeld = SIPlayer.Get(myGameEntity.lastHeldByActorNumber);
		if (lastPlayerHeld == SIPlayer.LocalPlayer)
		{
			localEverGrabbed = true;
		}
	}

	protected virtual void OnEnable()
	{
		GameEntity gameEntity = myGameEntity;
		gameEntity.OnSnapped = (Action)Delegate.Combine(gameEntity.OnSnapped, new Action(GrabInitialization));
		GameEntity gameEntity2 = myGameEntity;
		gameEntity2.OnGrabbed = (Action)Delegate.Combine(gameEntity2.OnGrabbed, new Action(GrabInitialization));
		GameEntity gameEntity3 = myGameEntity;
		gameEntity3.OnReleased = (Action)Delegate.Combine(gameEntity3.OnReleased, new Action(ReleaseInitialization));
		GameEntity gameEntity4 = myGameEntity;
		gameEntity4.OnUnsnapped = (Action)Delegate.Combine(gameEntity4.OnUnsnapped, new Action(ReleaseInitialization));
		timeReleased = Time.time;
		_rb.isKinematic = true;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void OnDisable()
	{
		GameEntity gameEntity = myGameEntity;
		gameEntity.OnSnapped = (Action)Delegate.Remove(gameEntity.OnSnapped, new Action(GrabInitialization));
		GameEntity gameEntity2 = myGameEntity;
		gameEntity2.OnGrabbed = (Action)Delegate.Remove(gameEntity2.OnGrabbed, new Action(GrabInitialization));
		GameEntity gameEntity3 = myGameEntity;
		gameEntity3.OnReleased = (Action)Delegate.Remove(gameEntity3.OnReleased, new Action(ReleaseInitialization));
		GameEntity gameEntity4 = myGameEntity;
		gameEntity4.OnUnsnapped = (Action)Delegate.Remove(gameEntity4.OnUnsnapped, new Action(ReleaseInitialization));
		SpawnRegion<GameEntity, SIResourceRegion>.RemoveItemFromRegion(myGameEntity);
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void GrabInitialization()
	{
		isSleeping = false;
		shouldSleep = false;
	}

	public void ReleaseInitialization()
	{
		shouldSleep = true;
		isSleeping = false;
		timeReleased = Time.time;
	}

	public virtual bool CanDeposit()
	{
		if (lastPlayerHeld != null && lastPlayerHeld.gamePlayer.IsLocal() && !localDeposited)
		{
			return SIPlayer.LocalPlayer.CanLimitedResourceBeDeposited(limitedDepositType);
		}
		return false;
	}

	public virtual void HandleDepositLocal(SIPlayer depositingPlayer)
	{
		localDeposited = true;
	}

	public virtual void HandleDepositAuth(SIPlayer depositingPlayer)
	{
	}

	private void HandleOnDestroyed(GameEntity entity)
	{
		if (localEverGrabbed && !localDeposited && entity.manager.IsZoneActive() && PhotonNetwork.InRoom)
		{
			if (type == ResourceType.StrangeWood)
			{
				PlayerGameEvents.MiscEvent("SIHelpOtherCollectStrangeWood");
			}
			else if (type == ResourceType.WeirdGear)
			{
				PlayerGameEvents.MiscEvent("SIHelpOtherCollectWeirdGears");
			}
			else if (type == ResourceType.FloppyMetal)
			{
				PlayerGameEvents.MiscEvent("SIHelpOtherCollectFloppyMetal");
			}
			else if (type == ResourceType.BouncySand)
			{
				PlayerGameEvents.MiscEvent("SIHelpOtherCollectBouncySand");
			}
			else if (type == ResourceType.VibratingSpring)
			{
				PlayerGameEvents.MiscEvent("SIHelpOtherCollectVibratingSpring");
			}
		}
	}

	public static List<ResourceCost> GetSum(params IList<ResourceCost>[] costs)
	{
		List<ResourceCost> list = new List<ResourceCost>();
		if (costs == null)
		{
			return list;
		}
		foreach (IList<ResourceCost> list2 in costs)
		{
			if (list2 == null)
			{
				continue;
			}
			foreach (ResourceCost item in list2)
			{
				list.AddResourceCost(item);
			}
		}
		return list;
	}

	public static List<ResourceCost> GetMax(params IList<ResourceCost>[] costs)
	{
		List<ResourceCost> list = new List<ResourceCost>();
		if (costs == null)
		{
			return list;
		}
		for (int i = 0; i < costs.Length; i++)
		{
			foreach (ResourceCost item in costs[i])
			{
				int amount = Mathf.Max(list.GetAmount(item.type), item.amount);
				list.SetAmount(item.type, amount);
			}
		}
		return list;
	}

	public static bool CategoryCostsMatch(IList<ResourceCost> cost1, IList<ResourceCost> cost2)
	{
		return cost1.GetCategoryCosts() == cost2.GetCategoryCosts();
	}

	public static bool CostsAreEqual(IList<ResourceCost> cost1, IList<ResourceCost> cost2, bool matchOrder = true)
	{
		if (cost1.Count != cost2.Count)
		{
			return false;
		}
		if (matchOrder)
		{
			for (int i = 0; i < cost1.Count; i++)
			{
				if (!cost1[i].Equals(cost2[i]))
				{
					return false;
				}
			}
		}
		else
		{
			foreach (ResourceCost item in cost1)
			{
				if (cost2.GetAmount(item.type) != item.amount)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static ResourceCost[] GenerateCostsFrom(Dictionary<ResourceType, int> costDictionary)
	{
		List<ResourceCost> list = new List<ResourceCost>();
		foreach (KeyValuePair<ResourceType, int> item in costDictionary)
		{
			list.Add(new ResourceCost(item.Key, item.Value));
		}
		list.Sort();
		return list.ToArray();
	}

	public static string PrintCost(IEnumerable<ResourceCost> costs)
	{
		return "[" + string.Join(", ", costs) + "]";
	}
}
