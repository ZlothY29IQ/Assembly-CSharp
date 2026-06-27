using System;
using UnityEngine;

namespace GorillaNetworking;

public class CosmeticsThrottler : MonoBehaviour, IGorillaSliceableSimple
{
	public enum RigDrawState
	{
		All = 0,
		Partial = 1,
		Min = 2,
		Startup = -1
	}

	public float DrawAllDistance = 5f;

	public float MaxDrawDistance = 10f;

	public bool DrawOnPlayerCount = true;

	public int DrawAllCount = 6;

	public int DrawMaxCount = 14;

	public int ThrottlePlayerCountThreshold = 11;

	private int lastPlayerCount;

	public CosmeticsController.CosmeticSlots[] ToggleSlots;

	[SerializeField]
	private GorillaRigHelper[] _rigHelpers;

	private int _cosmeticSlots;

	private float _update;

	private Camera mainCamera;

	private void Awake()
	{
		_cosmeticSlots = 16;
		VRRig[] allRigs = VRRigCache.Instance.GetAllRigs();
		_rigHelpers = new GorillaRigHelper[allRigs.Length];
		for (int i = 0; i < allRigs.Length; i++)
		{
			_rigHelpers[i] = new GorillaRigHelper
			{
				rig = allRigs[i],
				state = RigDrawState.Startup,
				sqrDistance = 9999f,
				prevSqrDistance = 9999f
			};
		}
		RoomSystem.JoinedRoomEvent += new Action(UpdatePlayerCount);
		RoomSystem.LeftRoomEvent += new Action(UpdatePlayerCount);
	}

	private void UpdatePlayerCount()
	{
		int num = NetworkSystem.Instance.AllNetPlayers.Length;
		if (num < ThrottlePlayerCountThreshold && lastPlayerCount >= ThrottlePlayerCountThreshold)
		{
			EnableAllRenderers();
		}
		lastPlayerCount = num;
	}

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.position, DrawAllDistance);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, MaxDrawDistance);
	}

	public void SliceUpdate()
	{
		if (lastPlayerCount < ThrottlePlayerCountThreshold)
		{
			return;
		}
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
			return;
		}
		Vector3 position = base.transform.position;
		for (int i = 0; i < _rigHelpers.Length; i++)
		{
			_rigHelpers[i].prevSqrDistance = _rigHelpers[i].sqrDistance;
			if (!_rigHelpers[i].rig.isActiveAndEnabled || _rigHelpers[i].rig.isLocal)
			{
				_rigHelpers[i].sqrDistance = 9999f;
				continue;
			}
			Vector3 position2 = _rigHelpers[i].rig.transform.position;
			if (mainCamera.WorldToScreenPoint(position2).z <= 0f)
			{
				_rigHelpers[i].sqrDistance = 9999f;
				continue;
			}
			float sqrMagnitude = (position2 - position).sqrMagnitude;
			_rigHelpers[i].sqrDistance = sqrMagnitude;
		}
		Array.Sort(_rigHelpers);
		float num = DrawAllDistance * DrawAllDistance;
		float num2 = MaxDrawDistance * MaxDrawDistance;
		for (int j = 0; j < _rigHelpers.Length; j++)
		{
			if (_rigHelpers[j].sqrDistance >= 9999f)
			{
				_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.Min);
				continue;
			}
			if (DrawOnPlayerCount)
			{
				if (j < DrawAllCount)
				{
					_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.All);
					continue;
				}
				if (j >= DrawMaxCount)
				{
					_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.Min);
					continue;
				}
			}
			if (_rigHelpers[j].sqrDistance <= num)
			{
				_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.All);
			}
			else if (_rigHelpers[j].sqrDistance <= num2)
			{
				_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.Partial);
			}
			else
			{
				_rigHelpers[j] = UpdateRigState(_rigHelpers[j], RigDrawState.Min);
			}
		}
	}

	private GorillaRigHelper UpdateRigState(GorillaRigHelper helper, RigDrawState newState)
	{
		RigDrawState state = helper.state;
		if (newState == state)
		{
			return helper;
		}
		switch (newState)
		{
		case RigDrawState.All:
			if (state != RigDrawState.All)
			{
				ToggleRenderersOnRig(helper.rig, toggle: true);
				helper.rig.ToggleMatParticles(enabled: true);
			}
			break;
		case RigDrawState.Partial:
			if (state <= RigDrawState.All)
			{
				ToggleRenderersOnRigForSlots(helper.rig, toggle: false);
				helper.rig.ToggleMatParticles(enabled: false);
			}
			else if (state == RigDrawState.Min)
			{
				ToggleRenderersOnRigForSlots(helper.rig, toggle: true, includesSlots: false);
			}
			break;
		case RigDrawState.Min:
			if (state != RigDrawState.Min)
			{
				ToggleRenderersOnRig(helper.rig, toggle: false);
				helper.rig.ToggleMatParticles(enabled: false);
			}
			break;
		}
		helper.state = newState;
		return helper;
	}

	private void ToggleRenderersOnRig(VRRig rig, bool toggle)
	{
		CosmeticsController.CosmeticSet cosmeticSet = rig.cosmeticSet;
		int num = cosmeticSet.items.Length;
		for (int i = 0; i < num; i++)
		{
			CosmeticItemInstance cosmeticItemInstance = rig.cosmeticsObjectRegistry.Cosmetic(cosmeticSet.items[i].displayName);
			if (cosmeticItemInstance != null)
			{
				cosmeticItemInstance.ToggleRenderers(toggle);
				cosmeticItemInstance.ToggleParticles(toggle);
			}
		}
	}

	private void ToggleRenderersOnRigForSlots(VRRig rig, bool toggle, bool includesSlots = true)
	{
		CosmeticsController.CosmeticSet cosmeticSet = rig.cosmeticSet;
		int num = cosmeticSet.items.Length;
		for (int i = 0; i < num; i++)
		{
			CosmeticItemInstance cosmeticItemInstance = rig.cosmeticsObjectRegistry.Cosmetic(cosmeticSet.items[i].displayName);
			if (cosmeticItemInstance != null)
			{
				cosmeticItemInstance.ToggleParticles(toggle);
				if (ContainsSlot(cosmeticItemInstance.ActiveSlot) == includesSlots)
				{
					cosmeticItemInstance.ToggleRenderers(toggle);
				}
			}
		}
	}

	private bool ContainsSlot(CosmeticsController.CosmeticSlots slot)
	{
		for (int i = 0; i < ToggleSlots.Length; i++)
		{
			if (ToggleSlots[i] == slot)
			{
				return true;
			}
		}
		return false;
	}

	public void EnableAllRenderers()
	{
		for (int i = 0; i < _rigHelpers.Length; i++)
		{
			ToggleRenderersOnRig(_rigHelpers[i].rig, toggle: true);
		}
	}
}
