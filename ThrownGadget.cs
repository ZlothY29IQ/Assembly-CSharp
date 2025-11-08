using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class ThrownGadget : MonoBehaviour
{
	public GameEntity gameEntity;

	private bool isHeldLocal;

	private bool lastThrowerLocal;

	private bool activationButtonLastInput;

	public event Action OnActivated;

	public event Action OnThrown;

	public event Action OnHitSurface;

	private void OnEnable()
	{
		isHeldLocal = false;
		lastThrowerLocal = false;
	}

	public bool IsHeld()
	{
		return gameEntity.heldByActorNumber != -1;
	}

	public bool IsHeldLocal()
	{
		return gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	public bool IsHeldByAnother()
	{
		if (IsHeld())
		{
			return !IsHeldLocal();
		}
		return false;
	}

	private bool IsButtonHeld()
	{
		if (!IsHeldLocal())
		{
			return false;
		}
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			if (out_gamePlayer == null)
			{
				return false;
			}
			int num = out_gamePlayer.FindHandIndex(gameEntity.id);
			if (num == -1)
			{
				return false;
			}
			return ControllerInputPoller.TriggerFloat(GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand) > 0.25f;
		}
		return false;
	}

	public void Update()
	{
		bool flag = IsHeldLocal();
		if (flag)
		{
			lastThrowerLocal = true;
			UpdateActivation();
		}
		else if (isHeldLocal)
		{
			this.OnThrown?.Invoke();
		}
		else if (IsHeldByAnother())
		{
			lastThrowerLocal = false;
		}
		isHeldLocal = flag;
	}

	private void UpdateActivation()
	{
		bool flag = IsButtonHeld();
		if (!activationButtonLastInput && flag)
		{
			this.OnActivated?.Invoke();
		}
		activationButtonLastInput = flag;
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (lastThrowerLocal)
		{
			this.OnHitSurface?.Invoke();
		}
	}
}
