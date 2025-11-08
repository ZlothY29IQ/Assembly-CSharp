using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RigEventVolume : MonoBehaviour
{
	private List<GameObject> gameObjects = new List<GameObject>();

	[SerializeField]
	private UnityEvent<VRRig> RigEnters;

	[SerializeField]
	private UnityEvent<VRRig> RigExits;

	[SerializeField]
	private UnityEvent UpTo1;

	[SerializeField]
	private UnityEvent DownTo0;

	[SerializeField]
	private UnityEvent UpTo2;

	[SerializeField]
	private UnityEvent DownTo1;

	[SerializeField]
	private UnityEvent UpTo3;

	[SerializeField]
	private UnityEvent DownTo2;

	[SerializeField]
	private UnityEvent UpTo4;

	[SerializeField]
	private UnityEvent DownTo3;

	[SerializeField]
	private UnityEvent UpTo5;

	[SerializeField]
	private UnityEvent DownTo4;

	[SerializeField]
	private UnityEvent UpTo6;

	[SerializeField]
	private UnityEvent DownTo5;

	[SerializeField]
	private UnityEvent UpTo7;

	[SerializeField]
	private UnityEvent DownTo6;

	[SerializeField]
	private UnityEvent UpTo8;

	[SerializeField]
	private UnityEvent DownTo7;

	[SerializeField]
	private UnityEvent UpTo9;

	[SerializeField]
	private UnityEvent DownTo8;

	[SerializeField]
	private UnityEvent UpTo10;

	[SerializeField]
	private UnityEvent DownTo9;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.TryGetComponent<VRRig>(out var component) && !gameObjects.Contains(other.gameObject))
		{
			gameObjects.Add(other.gameObject);
			countChanged(gameObjects.Count - 1, gameObjects.Count, component);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.TryGetComponent<VRRig>(out var component) && gameObjects.Contains(other.gameObject))
		{
			gameObjects.Remove(other.gameObject);
			countChanged(gameObjects.Count + 1, gameObjects.Count, component);
		}
	}

	private void countChanged(int oldValue, int newValue, VRRig rig)
	{
		if (newValue > oldValue)
		{
			RigEnters?.Invoke(rig);
			switch (newValue)
			{
			case 1:
				UpTo1?.Invoke();
				break;
			case 2:
				UpTo2?.Invoke();
				break;
			case 3:
				UpTo3?.Invoke();
				break;
			case 4:
				UpTo4?.Invoke();
				break;
			case 5:
				UpTo5?.Invoke();
				break;
			case 6:
				UpTo6?.Invoke();
				break;
			case 7:
				UpTo7?.Invoke();
				break;
			case 8:
				UpTo8?.Invoke();
				break;
			case 9:
				UpTo9?.Invoke();
				break;
			case 10:
				UpTo10?.Invoke();
				break;
			}
		}
		if (newValue < oldValue)
		{
			RigExits?.Invoke(rig);
			switch (newValue)
			{
			case 0:
				DownTo0?.Invoke();
				break;
			case 1:
				DownTo1?.Invoke();
				break;
			case 2:
				DownTo2?.Invoke();
				break;
			case 3:
				DownTo3?.Invoke();
				break;
			case 4:
				DownTo4?.Invoke();
				break;
			case 5:
				DownTo5?.Invoke();
				break;
			case 6:
				DownTo6?.Invoke();
				break;
			case 7:
				DownTo7?.Invoke();
				break;
			case 8:
				DownTo8?.Invoke();
				break;
			case 9:
				DownTo9?.Invoke();
				break;
			}
		}
	}
}
