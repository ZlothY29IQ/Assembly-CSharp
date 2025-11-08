using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class GameObjectManagerWithId : MonoBehaviour
{
	private class gameObjectData
	{
		public Transform transform;

		public Transform followTransform;

		public string id;

		public bool isMatched;
	}

	public GameObject objectsContainer;

	public GTZone zone;

	private readonly List<gameObjectData> objectData = new List<gameObjectData>();

	private void Awake()
	{
		Transform[] componentsInChildren = objectsContainer.GetComponentsInChildren<Transform>(includeInactive: false);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			gameObjectData gameObjectData = new gameObjectData();
			gameObjectData.transform = componentsInChildren[i];
			gameObjectData.id = zone.ToString() + i;
			objectData.Add(gameObjectData);
		}
	}

	private void OnDestroy()
	{
		objectData.Clear();
	}

	public void ReceiveEvent(string id, Transform _transform)
	{
		foreach (gameObjectData objectDatum in objectData)
		{
			if (objectDatum.id == id)
			{
				objectDatum.isMatched = true;
				objectDatum.followTransform = _transform;
			}
		}
	}

	private void Update()
	{
		foreach (gameObjectData objectDatum in objectData)
		{
			if (objectDatum.isMatched)
			{
				objectDatum.transform.transform.position = objectDatum.followTransform.position;
				objectDatum.transform.transform.rotation = objectDatum.followTransform.rotation;
			}
		}
	}
}
