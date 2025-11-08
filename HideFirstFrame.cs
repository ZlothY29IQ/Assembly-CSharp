using System.Collections;
using UnityEngine;

public class HideFirstFrame : MonoBehaviour
{
	[SerializeField]
	private int _frameDelay = 1;

	private Camera _cam;

	private float _farClipPlane;

	private void Awake()
	{
		_cam = GetComponent<Camera>();
		_farClipPlane = _cam.farClipPlane;
		_cam.farClipPlane = _cam.nearClipPlane + 0.1f;
	}

	public IEnumerator Start()
	{
		for (int i = 0; i < _frameDelay; i++)
		{
			yield return null;
		}
		_cam.farClipPlane = _farClipPlane;
	}
}
