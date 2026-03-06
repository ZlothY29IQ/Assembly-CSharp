using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigEventVolume : MonoBehaviour, IBuildValidation
{
	private enum Mode
	{
		RELATIVE,
		ABSOLUTE
	}

	private Dictionary<RigEventVolumeTrigger, int> gameObjects = new Dictionary<RigEventVolumeTrigger, int>();

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
	private UnityEvent<VRRig> RigEnters;

	[SerializeField]
	private UnityEvent<VRRig> RigExits;

	[SerializeField]
	private UnityEvent GoesOverThreshold;

	[SerializeField]
	private UnityEvent GoesUnderThreshold;

	[SerializeField]
	private UnityEvent<VRRig> LocalRigEnters;

	[SerializeField]
	private UnityEvent<VRRig> LocalRigExits;

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
		int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
		countChanged(gameObjects.Count, gameObjects.Count, num + 1, num, null);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component))
		{
			return;
		}
		if (!gameObjects.ContainsKey(component))
		{
			gameObjects.Add(component, 0);
			int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
			countChanged(gameObjects.Count - 1, gameObjects.Count, num, num, component);
			if (component.Rig == VRRig.LocalRig)
			{
				LocalRigEnters?.Invoke(component.Rig);
			}
		}
		else
		{
			gameObjects[component]++;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.gameObject.TryGetComponent<RigEventVolumeTrigger>(out var component))
		{
			return;
		}
		gameObjects[component]--;
		if (gameObjects[component] < 0)
		{
			gameObjects.Remove(component);
			int num = ((rigCollection == null) ? 1 : rigCollection.Rigs.Count);
			countChanged(gameObjects.Count + 1, gameObjects.Count, num, num, component);
			if (component.Rig == VRRig.LocalRig)
			{
				LocalRigExits?.Invoke(component.Rig);
			}
		}
	}

	private void countChanged(int oldValue, int newValue, int oldPlayerCount, int newPlayerCount, RigEventVolumeTrigger rig)
	{
		if (newValue > oldValue)
		{
			if (rig != null)
			{
				RigEnters?.Invoke(rig.Rig);
			}
			if ((mode == Mode.RELATIVE && (float)newValue / (float)newPlayerCount >= relThreshold && (float)oldValue / (float)oldPlayerCount < relThreshold) || (mode == Mode.ABSOLUTE && newValue >= absThreshold && oldValue < absThreshold))
			{
				GoesOverThreshold?.Invoke();
			}
		}
		else if (newValue < oldValue)
		{
			if (rig != null)
			{
				RigExits?.Invoke(rig.Rig);
			}
			if ((mode == Mode.RELATIVE && (float)newValue / (float)newPlayerCount < relThreshold && (float)oldValue / (float)oldPlayerCount >= relThreshold) || (mode == Mode.ABSOLUTE && newValue < absThreshold && oldValue >= absThreshold))
			{
				GoesUnderThreshold?.Invoke();
			}
		}
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (mode == Mode.RELATIVE && rigCollection == null)
		{
			Debug.Log("RigEventVolume on " + base.name + " is set to RELATIVE mode but has no Player Count Source. This will crash!");
			return false;
		}
		return true;
	}
}
