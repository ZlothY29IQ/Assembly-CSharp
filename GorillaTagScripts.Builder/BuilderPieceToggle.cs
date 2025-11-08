using System;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Builder;

public class BuilderPieceToggle : MonoBehaviour, IBuilderPieceFunctional, IBuilderPieceComponent, IBuilderTappable
{
	[Serializable]
	private enum ToggleType
	{
		OnTap,
		OnTriggerEnter
	}

	private enum ToggleStates
	{
		Off,
		On
	}

	[SerializeField]
	protected BuilderPiece myPiece;

	[SerializeField]
	private ToggleType toggleType;

	public bool onlySmallMonkeTaps;

	[SerializeField]
	private BuilderSmallHandTrigger[] handTriggers;

	[SerializeField]
	private BuilderSmallMonkeTrigger[] bodyTriggers;

	[SerializeField]
	protected UnityEvent ToggledOn;

	[SerializeField]
	protected UnityEvent ToggledOff;

	private List<Collider> colliders = new List<Collider>(5);

	private ToggleStates toggleState;

	private void Awake()
	{
		colliders.Clear();
		if (toggleType != ToggleType.OnTriggerEnter)
		{
			return;
		}
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
		foreach (BuilderSmallHandTrigger builderSmallHandTrigger in array)
		{
			if (!(builderSmallHandTrigger == null))
			{
				builderSmallHandTrigger.TriggeredEvent.RemoveListener(OnHandTriggerEntered);
			}
		}
		BuilderSmallMonkeTrigger[] array2 = bodyTriggers;
		foreach (BuilderSmallMonkeTrigger builderSmallMonkeTrigger in array2)
		{
			if (!(builderSmallMonkeTrigger == null))
			{
				builderSmallMonkeTrigger.onPlayerEnteredTrigger -= OnBodyTriggerEntered;
			}
		}
	}

	private bool CanTap()
	{
		if (onlySmallMonkeTaps && myPiece.GetTable().isTableMutable && (double)VRRigCache.Instance.localRig.Rig.scaleFactor > 0.99)
		{
			return false;
		}
		if (toggleType == ToggleType.OnTap)
		{
			return myPiece.state == BuilderPiece.State.AttachedAndPlaced;
		}
		return false;
	}

	public void OnTapLocal(float tapStrength)
	{
		if (!CanTap())
		{
			Debug.Log("BuilderPieceToggle Can't Tap");
			return;
		}
		Debug.Log("Tap Local");
		ToggleStateRequest();
	}

	private bool CanTrigger()
	{
		if (toggleType == ToggleType.OnTriggerEnter)
		{
			return myPiece.state == BuilderPiece.State.AttachedAndPlaced;
		}
		return false;
	}

	private void OnHandTriggerEntered()
	{
		if (CanTrigger())
		{
			ToggleStateRequest();
		}
		else
		{
			Debug.Log("BuilderPieceToggle Can't Trigger");
		}
	}

	private void OnBodyTriggerEntered(int playerNumber)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(playerNumber);
		if (player != null)
		{
			if (CanTrigger())
			{
				ToggleStateMaster(player.GetPlayerRef());
			}
			else
			{
				Debug.Log("BuilderPieceToggle Can't Trigger");
			}
		}
	}

	private void ToggleStateRequest()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			ToggleStates toggleStates = ((toggleState == ToggleStates.Off) ? ToggleStates.On : ToggleStates.Off);
			Debug.Log("BuilderPieceToggle" + $" Requesting state {toggleStates}");
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, (byte)toggleStates);
		}
	}

	private void ToggleStateMaster(Player instigator)
	{
		ToggleStates toggleStates = ((toggleState == ToggleStates.Off) ? ToggleStates.On : ToggleStates.Off);
		Debug.Log("BuilderPieceToggle" + $" Set Master state {toggleStates}");
		myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, (byte)toggleStates, instigator, NetworkSystem.Instance.ServerTimestamp);
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!IsStateValid(newState))
		{
			Debug.Log("BuilderPieceToggle State Invalid");
			return;
		}
		Debug.Log("BuilderPieceToggle" + $" State Changed {newState}");
		if ((ToggleStates)newState != toggleState)
		{
			if (newState == 1)
			{
				Debug.Log("BuilderPieceToggle Toggled On");
				ToggledOn?.Invoke();
			}
			else
			{
				Debug.Log("BuilderPieceToggle Toggled Off");
				ToggledOff.Invoke();
			}
		}
		toggleState = (ToggleStates)newState;
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		if (!IsStateValid(newState) || instigator == null)
		{
			Debug.Log("BuilderPieceToggle State Invalid or Player Null");
			return;
		}
		Debug.Log("BuilderPieceToggle" + $" State Request {newState}");
		if (newState != (byte)toggleState)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
		else
		{
			Debug.Log("BuilderPieceToggle Same State");
		}
	}

	public bool IsStateValid(byte state)
	{
		Debug.Log($"Is State Valid? {state}");
		return state <= 1;
	}

	public void FunctionalPieceUpdate()
	{
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		foreach (Collider collider in colliders)
		{
			collider.enabled = true;
		}
	}

	public void OnPieceDeactivate()
	{
		myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
		foreach (Collider collider in colliders)
		{
			collider.enabled = false;
		}
	}
}
