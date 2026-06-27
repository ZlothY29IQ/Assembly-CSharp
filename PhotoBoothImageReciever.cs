using System;
using UnityEngine;

public class PhotoBoothImageReciever : MonoBehaviour
{
	[SerializeField]
	private PhotoBoothCamera photoBoothCamera;

	[SerializeField]
	private int index = -1;

	private void OnEnable()
	{
		PhotoBoothCamera obj = photoBoothCamera;
		obj.OnCapture = (Action<Texture, int>)Delegate.Combine(obj.OnCapture, new Action<Texture, int>(photoBoothCamera_OnCapture));
	}

	private void photoBoothCamera_OnCapture(Texture texture, int i)
	{
		if (index < 0 || index == i)
		{
			GetComponent<Renderer>().material.mainTexture = texture;
		}
	}

	private void OnDisable()
	{
		PhotoBoothCamera obj = photoBoothCamera;
		obj.OnCapture = (Action<Texture, int>)Delegate.Remove(obj.OnCapture, new Action<Texture, int>(photoBoothCamera_OnCapture));
	}
}
