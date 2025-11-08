using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class BackpackGrabbableCosmetic : HoldableObject
{
	[GorillaSoundLookup]
	public int materialIndex;

	[SerializeField]
	private bool useCapacity = true;

	[SerializeField]
	private float coolDownTimer = 2f;

	[SerializeField]
	private int maxCapacity;

	[SerializeField]
	private int startItemsCount;

	[Space]
	public UnityEvent OnReachedMaxCapacity;

	public UnityEvent OnFullyEmptied;

	public UnityEvent OnRefilled;

	private int currentItemsCount;

	private bool canGrab;

	private float lastGrabTime;

	private void Awake()
	{
		currentItemsCount = startItemsCount;
		canGrab = true;
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void DropItemCleanup()
	{
	}

	public void Update()
	{
		if (!canGrab && Time.time - lastGrabTime >= coolDownTimer)
		{
			canGrab = true;
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (IsEmpty())
		{
			Debug.LogWarning("Can't remove item, Backpack is empty, need to refill.");
		}
		else if (canGrab)
		{
			lastGrabTime = Time.time;
			canGrab = false;
			((grabbingHand == EquipmentInteractor.instance.leftHand) ? SnowballMaker.leftHandInstance : SnowballMaker.rightHandInstance).TryCreateSnowball(materialIndex, out var _);
			RemoveItem();
		}
	}

	public void AddItem()
	{
		if (useCapacity)
		{
			if (maxCapacity <= currentItemsCount)
			{
				Debug.LogWarning("Can't add item, backpack is at full capacity.");
				return;
			}
			currentItemsCount++;
			UpdateState();
		}
	}

	public void RemoveItem()
	{
		if (useCapacity)
		{
			if (currentItemsCount < 0)
			{
				Debug.LogWarning("Can't remove item, Backpack is empty.");
				return;
			}
			currentItemsCount--;
			UpdateState();
		}
	}

	public void RefillBackpack()
	{
		if (useCapacity && currentItemsCount != startItemsCount)
		{
			currentItemsCount = startItemsCount;
			UpdateState();
		}
	}

	public void EmptyBackpack()
	{
		if (useCapacity && currentItemsCount != 0)
		{
			currentItemsCount = 0;
			UpdateState();
		}
	}

	public bool IsFull()
	{
		if (!useCapacity || maxCapacity == currentItemsCount)
		{
			return true;
		}
		return false;
	}

	public bool IsEmpty()
	{
		if (useCapacity && currentItemsCount == 0)
		{
			return true;
		}
		return false;
	}

	private void UpdateState()
	{
		if (useCapacity)
		{
			if (currentItemsCount == maxCapacity)
			{
				OnReachedMaxCapacity?.Invoke();
			}
			else if (currentItemsCount == 0)
			{
				OnFullyEmptied?.Invoke();
			}
			else if (currentItemsCount == startItemsCount)
			{
				OnRefilled?.Invoke();
			}
		}
	}
}
