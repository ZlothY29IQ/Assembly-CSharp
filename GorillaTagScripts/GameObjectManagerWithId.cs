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
			gameObjectData gameObjectData2 = new gameObjectData();
			gameObjectData2.transform = componentsInChildren[i];
			gameObjectData2.id = zone.ToString() + i;
			objectData.Add(gameObjectData2);
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
