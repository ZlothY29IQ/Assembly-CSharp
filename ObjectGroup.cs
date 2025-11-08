using System.Collections.Generic;
using UnityEngine;

public class ObjectGroup : MonoBehaviour
{
	public List<GameObject> gameObjects = new List<GameObject>(16);

	public List<Behaviour> behaviours = new List<Behaviour>(16);

	public List<Renderer> renderers = new List<Renderer>(16);

	public List<Collider> colliders = new List<Collider>(16);

	public bool syncWithGroupState = true;

	private void OnEnable()
	{
		if (syncWithGroupState)
		{
			SetObjectStates(active: true);
		}
	}

	private void OnDisable()
	{
		if (syncWithGroupState)
		{
			SetObjectStates(active: false);
		}
	}

	public void SetObjectStates(bool active)
	{
		int count = gameObjects.Count;
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = gameObjects[i];
			if (!(gameObject == null))
			{
				gameObject.SetActive(active);
			}
		}
		int count2 = behaviours.Count;
		for (int j = 0; j < count2; j++)
		{
			Behaviour behaviour = behaviours[j];
			if (!(behaviour == null))
			{
				behaviour.enabled = active;
			}
		}
		int count3 = renderers.Count;
		for (int k = 0; k < count3; k++)
		{
			Renderer renderer = renderers[k];
			if (!(renderer == null))
			{
				renderer.enabled = active;
			}
		}
		int count4 = colliders.Count;
		for (int l = 0; l < count4; l++)
		{
			Collider collider = colliders[l];
			if (!(collider == null))
			{
				collider.enabled = active;
			}
		}
	}
}
