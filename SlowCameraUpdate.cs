using System.Collections;
using UnityEngine;

public class SlowCameraUpdate : MonoBehaviour
{
	private Camera myCamera;

	private float frameRate;

	private float timeToNextFrame;

	public void Awake()
	{
		frameRate = 30f;
		timeToNextFrame = 1f / frameRate;
		myCamera = GetComponent<Camera>();
	}

	public void OnEnable()
	{
		StartCoroutine(UpdateMirror());
	}

	public void OnDisable()
	{
		StopAllCoroutines();
	}

	public IEnumerator UpdateMirror()
	{
		while (true)
		{
			if (base.gameObject.activeSelf)
			{
				Debug.Log("rendering camera!");
				myCamera.Render();
			}
			yield return new WaitForSeconds(timeToNextFrame);
		}
	}
}
