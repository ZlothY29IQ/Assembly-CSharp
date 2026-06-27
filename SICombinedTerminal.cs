using System.Collections.Generic;
using System.IO;
using GorillaTag;
using Photon.Pun;
using UnityEngine;

public class SICombinedTerminal : MonoBehaviour, IGorillaSliceableSimple
{
	public enum TerminalSubFunction
	{
		TechTree,
		GadgetDispenser,
		ResourceCollection
	}

	[DebugReadout]
	internal int index;

	[DebugReadout]
	internal SIPlayer activePlayer;

	[DebugReadout]
	internal bool isOccupiedByActivePlayer;

	[DebugReadout]
	internal bool isOccupiedByLocalPlayer;

	[DebugReadout]
	internal bool isOccupied;

	[DebugReadout]
	internal bool wasOccupied;

	[DebugReadout]
	internal SuperInfection superInfection;

	public SIGadgetDispenser dispenser;

	public SITechTreeStation techTree;

	public SIResourceCollection resourceCollection;

	[SerializeField]
	private GTAnimator[] m_gtAnimators;

	public Collider activeUserBounds;

	public float foldupDelay = 20f;

	private float foldupTimeStart;

	private EKioskAnimState state;

	[DebugReadout]
	private int _activePage;

	[Header("Flattener")]
	public Transform zeroZeroImage;

	public Transform onePointTwoText;

	private List<VRRig> rigs = new List<VRRig>();

	public AudioSource wrongPlayerBuzz;

	public bool IsAuthority => superInfection.siManager.gameEntityManager.IsAuthority();

	public SuperInfectionManager SIManager => superInfection.siManager;

	public int ActivePage => _activePage;

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		wasOccupied = isOccupied;
		isOccupied = false;
		isOccupiedByActivePlayer = false;
		VRRigCache.Instance.GetActiveRigs(rigs);
		for (int i = 0; i < rigs.Count; i++)
		{
			if (activeUserBounds.bounds.Contains(rigs[i].transform.position))
			{
				isOccupied = true;
				if (rigs[i].Creator.IsLocal)
				{
					isOccupiedByActivePlayer = true;
					break;
				}
			}
		}
		if (isOccupied)
		{
			float num = Time.time - SIProgression.Instance.timeTelemetryLastChecked;
			if (activePlayer != null && activePlayer.ActorNr == SIPlayer.LocalPlayer.ActorNr && isOccupiedByLocalPlayer)
			{
				SIProgression.Instance.activeTerminalTimeInterval += num;
				SIProgression.Instance.activeTerminalTimeTotal += num;
			}
			if (!wasOccupied && state == EKioskAnimState.Closing)
			{
				AnimQueueState(EKioskAnimState.Opening);
			}
			foldupTimeStart = Time.time;
		}
		else if (state == EKioskAnimState.Opening && Time.time > foldupTimeStart + foldupDelay && !isOccupied)
		{
			AnimQueueState(EKioskAnimState.Closing);
		}
	}

	public void Reset()
	{
		activePlayer = null;
		SetActivePage(0);
		dispenser.Initialize();
		techTree.Initialize();
		resourceCollection.Initialize();
		dispenser.Reset();
		techTree.Reset();
		resourceCollection.Reset();
		AnimQueueState(EKioskAnimState.Closing);
	}

	public void Awake()
	{
		if (superInfection == null)
		{
			superInfection = GetComponentInParent<SuperInfection>();
		}
		dispenser.Initialize();
		techTree.Initialize();
		resourceCollection.Initialize();
		Reset();
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (activePlayer != null)
		{
			stream.SendNext(activePlayer.ActorNr);
		}
		else
		{
			stream.SendNext(-1);
		}
		stream.SendNext(_activePage);
		dispenser.WriteDataPUN(stream, info);
		techTree.WriteDataPUN(stream, info);
		resourceCollection.WriteDataPUN(stream, info);
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		activePlayer = SIPlayer.Get((int)stream.ReceiveNext());
		_activePage = (int)stream.ReceiveNext();
		dispenser.ReadDataPUN(stream, info);
		techTree.ReadDataPUN(stream, info);
		resourceCollection.ReadDataPUN(stream, info);
	}

	public void SerializeZoneData(BinaryWriter writer)
	{
		writer.Write(_activePage);
		dispenser.ZoneDataSerializeWrite(writer);
		techTree.ZoneDataSerializeWrite(writer);
		resourceCollection.ZoneDataSerializeWrite(writer);
	}

	public void DeserializeZoneData(BinaryReader reader)
	{
		_activePage = reader.ReadInt32();
		SetActivePage(_activePage);
		dispenser.ZoneDataSerializeRead(reader);
		techTree.ZoneDataSerializeRead(reader);
		resourceCollection.ZoneDataSerializeRead(reader);
	}

	public void PlayerHandScanned(int actorNr)
	{
		if (!IsAuthority)
		{
			superInfection.siManager.CallRPC(SuperInfectionManager.ClientToAuthorityRPC.CombinedTerminalHandScan, new object[1] { index });
			return;
		}
		SIPlayer sIPlayer = SIPlayer.Get(actorNr);
		if (!(activePlayer != null) || !activePlayer.isActiveAndEnabled || !(sIPlayer != activePlayer) || !activeUserBounds.bounds.Contains(activePlayer.transform.position))
		{
			activePlayer = sIPlayer;
			dispenser.PlayerHandScanned(actorNr);
			techTree.PlayerHandScanned(actorNr);
			resourceCollection.PlayerHandScanned(actorNr);
		}
	}

	public void TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr, TerminalSubFunction subFunction)
	{
		if (!IsAuthority)
		{
			SIManager.CallRPC(SuperInfectionManager.ClientToAuthorityRPC.CombinedTerminalButtonPress, new object[4]
			{
				(int)buttonType,
				data,
				(int)subFunction,
				index
			});
			return;
		}
		switch (subFunction)
		{
		case TerminalSubFunction.GadgetDispenser:
			dispenser.TouchscreenButtonPressed(buttonType, data, actorNr);
			break;
		case TerminalSubFunction.TechTree:
			techTree.TouchscreenButtonPressed(buttonType, data, actorNr);
			break;
		case TerminalSubFunction.ResourceCollection:
			resourceCollection.TouchscreenButtonPressed(buttonType, data, actorNr);
			break;
		}
	}

	public void SetActivePage(int pageId)
	{
		_activePage = pageId;
		if (techTree.IsValidPage(pageId))
		{
			techTree.SetActivePage();
		}
		if (dispenser.IsValidPage(pageId))
		{
			dispenser.SetActivePage();
		}
	}

	private void AnimQueueState(EKioskAnimState newState)
	{
		state = newState;
		for (int i = 0; i < m_gtAnimators.Length; i++)
		{
			if (!(m_gtAnimators[i] == null))
			{
				m_gtAnimators[i].QueueState((long)newState);
			}
		}
	}

	public void PlayWrongPlayerBuzz(Transform xForm)
	{
		wrongPlayerBuzz.transform.position = xForm.position;
		wrongPlayerBuzz.PlayOneShot(wrongPlayerBuzz.clip);
	}
}
