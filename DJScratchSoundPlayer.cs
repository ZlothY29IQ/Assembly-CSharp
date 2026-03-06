using System;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class DJScratchSoundPlayer : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private SoundBankPlayer scratchForward;

	[SerializeField]
	private SoundBankPlayer scratchBack;

	[SerializeField]
	private SoundBankPlayer scratchPause;

	[SerializeField]
	private SoundBankPlayer scratchResume;

	[SerializeField]
	private DJScratchtable scratchTableLeft;

	[SerializeField]
	private DJScratchtable scratchTableRight;

	private RubberDuckEvents _events;

	private VRRig myRig;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnDespawn()
	{
	}

	private void OnEnable()
	{
		if (_events.IsNull())
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((!(myRig != null)) ? null : ((myRig.creator != null) ? myRig.creator : NetworkSystem.Instance.LocalPlayer));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnPlayEvent);
	}

	private void OnDisable()
	{
		if (_events.IsNotNull())
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnPlayEvent);
			_events.Dispose();
			_events = null;
		}
	}

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
		if (!rig.isLocal)
		{
			scratchTableLeft.enabled = false;
			scratchTableRight.enabled = false;
		}
	}

	public void Play(ScratchSoundType type, bool isLeft)
	{
		if (myRig.isLocal)
		{
			PlayLocal(type, isLeft);
			_events.Activate.RaiseOthers((int)(type + (isLeft ? 100 : 0)));
		}
	}

	public void OnPlayEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target || info.senderID != myRig.creator.ActorNumber)
		{
			return;
		}
		if (args.Length != 1)
		{
			Debug.LogError($"Invalid DJ Scratch Event - expected 1 arg, got {args.Length}");
			return;
		}
		int num = (int)args[0];
		bool flag = num >= 100;
		if (flag)
		{
			num -= 100;
		}
		ScratchSoundType scratchSoundType = (ScratchSoundType)num;
		if (scratchSoundType >= ScratchSoundType.Pause && scratchSoundType <= ScratchSoundType.Back)
		{
			PlayLocal(scratchSoundType, flag);
		}
	}

	public void PlayLocal(ScratchSoundType type, bool isLeft)
	{
		switch (type)
		{
		case ScratchSoundType.Pause:
			(isLeft ? scratchTableLeft : scratchTableRight).PauseTrack();
			scratchPause.Play();
			break;
		case ScratchSoundType.Resume:
			(isLeft ? scratchTableLeft : scratchTableRight).ResumeTrack();
			scratchResume.Play();
			break;
		case ScratchSoundType.Forward:
			scratchForward.Play();
			(isLeft ? scratchTableLeft : scratchTableRight).PauseTrack();
			break;
		case ScratchSoundType.Back:
			scratchBack.Play();
			(isLeft ? scratchTableLeft : scratchTableRight).PauseTrack();
			break;
		}
	}
}
