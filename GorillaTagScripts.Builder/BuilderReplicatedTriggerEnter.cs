using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Builder;

public class BuilderReplicatedTriggerEnter : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	private enum FunctionalState
	{
		Idle,
		TriggerEntered
	}

	[SerializeField]
	protected BuilderPiece myPiece;

	[Tooltip("How long in seconds to wait between trigger events")]
	[SerializeField]
	protected float triggerCooldown = 0.5f;

	[SerializeField]
	private BuilderSmallHandTrigger[] handTriggers;

	[SerializeField]
	private BuilderSmallMonkeTrigger[] bodyTriggers;

	[Tooltip("Optional Animation to play when triggered")]
	[SerializeField]
	private Animation animationOnTrigger;

	[Tooltip("Optional Sound to play when triggered")]
	[SerializeField]
	private SoundBankPlayer activateSoundBank;

	[Tooltip("Knockback the triggering player?")]
	[SerializeField]
	private bool knockbackOnTriggerEnter;

	[SerializeField]
	private float knockbackVelocity;

	[Tooltip("uses Forward of the transform provided")]
	[SerializeField]
	private Transform knockbackDirection;

	private List<Collider> colliders = new List<Collider>(5);

	private bool isPieceActive;

	private float lastTriggerTime;

	private FunctionalState currentState;

	public UnityEvent OnTriggered;

	private void Awake()
	{
		colliders.Clear();
		BuilderSmallHandTrigger[] array = handTriggers;
		foreach (BuilderSmallHandTrigger obj in array)
		{
			obj.TriggeredEvent.AddListener(OnHandTriggerEntered);
			Collider component = obj.GetComponent<Collider>();
			if (component != null)
			{
				colliders.Add(component);
			}
		}
		BuilderSmallMonkeTrigger[] array2 = bodyTriggers;
		foreach (BuilderSmallMonkeTrigger obj2 in array2)
		{
			obj2.onPlayerEnteredTrigger += OnBodyTriggerEntered;
			Collider component2 = obj2.GetComponent<Collider>();
			if (component2 != null)
			{
				colliders.Add(component2);
			}
		}
	}

	private void OnDestroy()
	{
		BuilderSmallHandTrigger[] array = handTriggers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].TriggeredEvent.RemoveListener(OnHandTriggerEntered);
		}
		BuilderSmallMonkeTrigger[] array2 = bodyTriggers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].onPlayerEnteredTrigger -= OnBodyTriggerEntered;
		}
	}

	private void PlayTriggerEffects(NetPlayer target)
	{
		OnTriggered?.Invoke();
		if (animationOnTrigger != null && animationOnTrigger.clip != null)
		{
			animationOnTrigger.Rewind();
			animationOnTrigger.Play();
		}
		if (activateSoundBank != null)
		{
			activateSoundBank.Play();
		}
		if (!target.IsLocal)
		{
			return;
		}
		VRRig rig = VRRigCache.Instance.localRig.Rig;
		if (rig != null)
		{
			float num = 1.5f * rig.scaleFactor;
			if (!((rig.transform.position - base.transform.position).sqrMagnitude > num * num))
			{
				GTPlayer.Instance.SetMaximumSlipThisFrame();
				GTPlayer.Instance.ApplyKnockback(knockbackDirection.forward, knockbackVelocity * rig.scaleFactor);
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 2f, Time.fixedDeltaTime);
				GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 2f, Time.fixedDeltaTime);
			}
		}
	}

	private void OnHandTriggerEntered()
	{
		if (CanTrigger())
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 1);
		}
	}

	private void OnBodyTriggerEntered(int playerNumber)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(playerNumber);
			if (player != null && CanTrigger())
			{
				myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, player.GetPlayerRef(), NetworkSystem.Instance.ServerTimestamp);
			}
		}
	}

	private bool CanTrigger()
	{
		if (isPieceActive && currentState == FunctionalState.Idle)
		{
			return Time.time > lastTriggerTime + triggerCooldown;
		}
		return false;
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		currentState = FunctionalState.Idle;
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		isPieceActive = true;
		foreach (Collider collider in colliders)
		{
			collider.enabled = true;
		}
	}

	public void OnPieceDeactivate()
	{
		isPieceActive = false;
		if (currentState == FunctionalState.TriggerEntered)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
		foreach (Collider collider in colliders)
		{
			collider.enabled = false;
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (IsStateValid(newState))
		{
			if (newState == 1 && currentState != FunctionalState.TriggerEntered)
			{
				lastTriggerTime = Time.time;
				myPiece.GetTable().RegisterFunctionalPiece(this);
				PlayTriggerEffects(instigator);
			}
			currentState = (FunctionalState)newState;
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && newState == 1 && CanTrigger())
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public bool IsStateValid(byte state)
	{
		return state <= 1;
	}

	public void FunctionalPieceUpdate()
	{
		if (lastTriggerTime + triggerCooldown < Time.time)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
	}
}
