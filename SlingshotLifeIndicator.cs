using System;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class SlingshotLifeIndicator : MonoBehaviour, IGorillaSliceableSimple, ISpawnable
{
	private VRRig myRig;

	public GorillaPaintbrawlManager bMgr;

	public bool checkedBattle;

	public bool inBattle;

	public GameObject indicator1;

	public GameObject indicator2;

	public GameObject indicator3;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	void ISpawnable.OnDespawn()
	{
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		Reset();
		RoomSystem.LeftRoomEvent -= new Action(OnLeftRoom);
	}

	private void SetActive(GameObject obj, bool active)
	{
		if (!obj.activeSelf && active)
		{
			obj.SetActive(value: true);
		}
		if (obj.activeSelf && !active)
		{
			obj.SetActive(value: false);
		}
	}

	public void SliceUpdate()
	{
		if (NetworkSystem.Instance.InRoom && (!checkedBattle || inBattle))
		{
			if (bMgr == null)
			{
				checkedBattle = true;
				inBattle = true;
				if (GorillaGameManager.instance == null)
				{
					return;
				}
				bMgr = GorillaGameManager.instance.gameObject.GetComponent<GorillaPaintbrawlManager>();
				if (bMgr == null)
				{
					inBattle = false;
					return;
				}
			}
			if (myRig?.creator != null)
			{
				int playerLives = bMgr.GetPlayerLives(myRig.creator);
				SetActive(indicator1, playerLives >= 1);
				SetActive(indicator2, playerLives >= 2);
				SetActive(indicator3, playerLives >= 3);
			}
		}
		else
		{
			if (indicator1.activeSelf)
			{
				indicator1.SetActive(value: false);
			}
			if (indicator2.activeSelf)
			{
				indicator2.SetActive(value: false);
			}
			if (indicator3.activeSelf)
			{
				indicator3.SetActive(value: false);
			}
		}
	}

	public void OnLeftRoom()
	{
		Reset();
	}

	public void Reset()
	{
		bMgr = null;
		inBattle = false;
		checkedBattle = false;
	}
}
