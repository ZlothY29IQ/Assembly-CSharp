using System;
using GorillaTag;
using UnityEngine;

public class StopwatchCosmetic : TransferrableObject
{
	[SerializeField]
	private StopwatchFace _watchFace;

	[NonSerialized]
	[Space]
	private bool _isActivating;

	[NonSerialized]
	private float _activeTimeElapsed;

	[NonSerialized]
	private bool _activated;

	[NonSerialized]
	[Space]
	private int _photonID = -1;

	private static PhotonEvent gWatchToggleRPC;

	private static PhotonEvent gWatchResetRPC;

	private Action<int, int, object[], PhotonMessageInfoWrapped> _watchToggle;

	private Action<int, int, object[], PhotonMessageInfoWrapped> _watchReset;

	[DebugOption]
	public bool disableActivation;

	[DebugOption]
	public bool disableDeactivation;

	public bool isActivating => _isActivating;

	public float activeTimeElapsed => _activeTimeElapsed;

	protected override void Awake()
	{
		base.Awake();
		if (gWatchToggleRPC == null)
		{
			gWatchToggleRPC = new PhotonEvent(StaticHash.Compute("StopwatchCosmetic", "WatchToggle"));
		}
		if (gWatchResetRPC == null)
		{
			gWatchResetRPC = new PhotonEvent(StaticHash.Compute("StopwatchCosmetic", "WatchReset"));
		}
		_watchToggle = OnWatchToggle;
		_watchReset = OnWatchReset;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		if (!FetchMyViewID(out var viewID))
		{
			_photonID = -1;
			return;
		}
		gWatchResetRPC += _watchReset;
		gWatchToggleRPC += _watchToggle;
		_photonID = viewID.GetStaticHash();
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		gWatchResetRPC -= _watchReset;
		gWatchToggleRPC -= _watchToggle;
	}

	private void OnWatchToggle(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (_photonID != -1 && info.senderID == ownerRig.creator.ActorNumber && sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnWatchToggle");
			if ((int)args[0] == _photonID)
			{
				_ = (bool)args[1];
				int millis = (int)args[2];
				_watchFace.SetMillisElapsed(millis);
				_watchFace.WatchToggle();
			}
		}
	}

	private void OnWatchReset(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (_photonID != -1 && info.senderID == ownerRig.creator.ActorNumber && sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnWatchReset");
			if ((int)args[0] == _photonID)
			{
				_watchFace.WatchReset();
			}
		}
	}

	private bool FetchMyViewID(out int viewID)
	{
		viewID = -1;
		NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
		if (netPlayer == null)
		{
			return false;
		}
		if (!VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig))
		{
			return false;
		}
		if (playerRig.Rig.netView == null)
		{
			return false;
		}
		viewID = playerRig.Rig.netView.ViewID;
		return true;
	}

	public bool PollActivated()
	{
		if (!_activated)
		{
			return false;
		}
		_activated = false;
		return true;
	}

	public override void TriggeredLateUpdate()
	{
		base.TriggeredLateUpdate();
		if (_isActivating)
		{
			_activeTimeElapsed += Time.deltaTime;
		}
		if (_isActivating && _activeTimeElapsed > 1f)
		{
			_isActivating = false;
			_watchFace.WatchReset(doLerp: true);
			gWatchResetRPC.RaiseOthers(_photonID);
		}
	}

	public override void OnActivate()
	{
		if (CanActivate())
		{
			base.OnActivate();
			if (IsMyItem())
			{
				_activeTimeElapsed = 0f;
				_isActivating = true;
			}
		}
	}

	public override void OnDeactivate()
	{
		if (CanDeactivate())
		{
			base.OnDeactivate();
			if (IsMyItem())
			{
				_isActivating = false;
				_activated = true;
				_watchFace.WatchToggle();
				gWatchToggleRPC.RaiseOthers(_photonID, _watchFace.watchActive, _watchFace.millisElapsed);
				_activated = false;
			}
		}
	}

	public override bool CanActivate()
	{
		return !disableActivation;
	}

	public override bool CanDeactivate()
	{
		return !disableDeactivation;
	}
}
