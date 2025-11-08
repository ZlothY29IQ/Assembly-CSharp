using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRBay : MonoBehaviour
{
	public List<GameObject> hideWhenOpen;

	public List<GameObject> hideWhenClosed;

	public Animation bayDoorAnimation;

	private bool isOpen;

	public TMP_Text playerName;

	public TMP_Text maxDropText;

	public List<GameObject> showWhenOwned;

	public List<GameObject> showWhenNotOwned;

	public int unlockByDrillLevel = -1;

	public GRShuttleGroupLoc shuttleLoc = GRShuttleGroupLoc.Invalid;

	public int shuttleIndex = -1;

	[NonSerialized]
	public bool debugForceUnlockedByLevel;

	private GRShuttle unlockShuttle;

	private GhostReactor reactor;

	private void Awake()
	{
		if (playerName != null)
		{
			playerName.text = null;
		}
		if (maxDropText != null)
		{
			maxDropText.text = null;
		}
	}

	public void Setup(GhostReactor reactor)
	{
		this.reactor = reactor;
		if (shuttleLoc != GRShuttleGroupLoc.Invalid && shuttleIndex >= 0 && shuttleIndex < 10)
		{
			unlockShuttle = GRElevatorManager._instance.GetPlayerShuttle(shuttleLoc, shuttleIndex);
			if (unlockShuttle != null)
			{
				unlockShuttle.SetBay(this);
			}
		}
		Refresh();
	}

	public void SetOpen(bool open)
	{
		if (hideWhenOpen != null)
		{
			for (int i = 0; i < hideWhenOpen.Count; i++)
			{
				if (hideWhenOpen[i] != null)
				{
					hideWhenOpen[i].SetActive(!open);
					continue;
				}
				Debug.LogErrorFormat("Why is hideWhenOpen null {0} at {1}", base.gameObject.name, i);
			}
		}
		else
		{
			Debug.LogErrorFormat("Why is hideWhenOpen null {0}", base.gameObject.name);
		}
		if (hideWhenClosed != null)
		{
			for (int j = 0; j < hideWhenClosed.Count; j++)
			{
				if (hideWhenClosed[j] != null)
				{
					hideWhenClosed[j].SetActive(open);
					continue;
				}
				Debug.LogErrorFormat("Why is hideWhenClosed null {0} at {1} ", base.gameObject.name, j);
			}
		}
		else
		{
			Debug.LogErrorFormat("Why is hideWhenClosed null {0}", base.gameObject.name);
		}
		if (bayDoorAnimation != null && isOpen != open)
		{
			if (open)
			{
				bayDoorAnimation.Play("BayDoor_Open");
				bayDoorAnimation.PlayQueued("BayDoor_Open_Idle");
			}
			else
			{
				bayDoorAnimation.Play("BayDoor_Close");
				bayDoorAnimation.PlayQueued("BayDoor_Close_Idle");
			}
		}
		isOpen = open;
	}

	public void Refresh()
	{
		bool open = true;
		if (unlockShuttle != null)
		{
			NetPlayer owner = unlockShuttle.GetOwner();
			bool flag = owner != null && unlockShuttle.IsPodUnlocked();
			open = unlockShuttle.GetState() == GRShuttleState.Docked && flag;
			if (playerName != null)
			{
				playerName.text = ((!flag) ? null : owner.SanitizedNickName);
			}
			if (maxDropText != null)
			{
				int num = unlockShuttle.GetMaxDropFloor() + 1;
				maxDropText.text = ((!flag) ? null : num.ToString());
			}
			for (int i = 0; i < showWhenOwned.Count; i++)
			{
				showWhenOwned[i].SetActive(flag);
			}
			for (int j = 0; j < showWhenNotOwned.Count; j++)
			{
				showWhenNotOwned[j].SetActive(!flag);
			}
		}
		else if (unlockByDrillLevel > 0)
		{
			open = (reactor != null && reactor.GetDepthLevel() >= unlockByDrillLevel) || GhostReactorManager.bayUnlockEnabled;
		}
		SetOpen(open);
	}
}
