using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigEventGate : MonoBehaviour, IBuildValidation
{
	private enum Mode
	{
		RELATIVE,
		ABSOLUTE
	}

	private List<RigEventVolumeTrigger> gameObjects = new List<RigEventVolumeTrigger>();

	[SerializeField]
	private Mode mode = Mode.ABSOLUTE;

	[Range(0.05f, 1f)]
	[SerializeField]
	private float relThreshold = 0.05f;

	[SerializeField]
	private VRRigCollection rigCollection;

	[Range(1f, 20f)]
	[SerializeField]
	private int absThreshold = 1;

	[SerializeField]
	private UnityEvent<VRRig> RigExits;

	[SerializeField]
	private UnityEvent GoesOverThreshold;

	private void OnEnable()
	{
		if (!(rigCollection == null))
		{
			VRRigCollection vRRigCollection = rigCollection;
			vRRigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Combine(vRRigCollection.playerEnteredCollection, new Action<RigContainer>(OnJoined));
			VRRigCollection vRRigCollection2 = rigCollection;
			vRRigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Combine(vRRigCollection2.playerLeftCollection, new Action<RigContainer>(OnLeft));
		}
	}

	private void OnDisable()
	{
		if (!(rigCollection == null))
		{
			VRRigCollection vRRigCollection = rigCollection;
			vRRigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection.playerEnteredCollection, new Action<RigContainer>(OnJoined));
			VRRigCollection vRRigCollection2 = rigCollection;
			vRRigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection2.playerLeftCollection, new Action<RigContainer>(OnLeft));
		}
	}

	private void OnDestroy()
	{
		if (!(rigCollection == null))
		{
			VRRigCollection vRRigCollection = rigCollection;
			vRRigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection.playerEnteredCollection, new Action<RigContainer>(OnJoined));
			VRRigCollection vRRigCollection2 = rigCollection;
			vRRigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Remove(vRRigCollection2.playerLeftCollection, new Action<RigContainer>(OnLeft));
		}
	}

	private void OnJoined(RigContainer rc)
	{
		int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
		countChanged(gameObjects.Count, gameObjects.Count, num - 1, num, null);
	}

	private void OnLeft(RigContainer rc)
	{
		RigEventVolumeTrigger rigEventVolumeTrigger = null;
		for (int i = 0; i < gameObjects.Count; i++)
		{
			if (gameObjects[i].Rig == rc.Rig)
			{
				rigEventVolumeTrigger = gameObjects[i];
			}
		}
		int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
		if (rigEventVolumeTrigger != null)
		{
			gameObjects.Remove(rigEventVolumeTrigger);
			countChanged(gameObjects.Count + 1, gameObjects.Count, num + 1, num, null);
		}
		else
		{
			countChanged(gameObjects.Count, gameObjects.Count, num + 1, num, null);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component) && !(base.transform.InverseTransformPoint(component.transform.position).z < 0f) && gameObjects.Contains(component))
		{
			int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
			int count = gameObjects.Count;
			gameObjects.Remove(component);
			countChanged(count, gameObjects.Count, num, num, component);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component) && !(base.transform.InverseTransformPoint(component.transform.position).z < 0f) && !gameObjects.Contains(component))
		{
			int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
			int count = gameObjects.Count;
			gameObjects.Add(component);
			countChanged(count, gameObjects.Count, num, num, component);
		}
	}

	private void countChanged(int oldValue, int newValue, int oldPlayerCount, int newPlayerCount, RigEventVolumeTrigger rig)
	{
		if (newValue > oldValue)
		{
			if (rig != null)
			{
				RigExits?.Invoke(rig.Rig);
			}
			if ((mode == Mode.RELATIVE && (float)newValue / (float)newPlayerCount >= relThreshold && (float)oldValue / (float)oldPlayerCount < relThreshold) || (mode == Mode.ABSOLUTE && newValue >= absThreshold && oldValue < absThreshold))
			{
				GoesOverThreshold?.Invoke();
			}
		}
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (mode == Mode.RELATIVE && rigCollection == null)
		{
			Debug.Log("RigEventGate on " + base.name + " is set to RELATIVE mode but has no Player Count Source. This will crash!");
			return false;
		}
		return true;
	}
}
