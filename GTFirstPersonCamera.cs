using System;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(int.MinValue)]
public class GTFirstPersonCamera : MonoBehaviour
{
	private const string preLog = "[GTFirstPersonCamera]  ";

	private const string preErr = "[GTFirstPersonCamera]  ERROR!!!  ";

	public static Action OnPreRenderEvent;

	public static Camera camera { get; private set; }

	public void Awake()
	{
		camera = GetComponent<Camera>();
		if (camera == null)
		{
			Debug.LogError("[GTFirstPersonCamera]  ERROR!!!  Could not find Camera on same GameObject!");
		}
		else
		{
			RenderPipelineManager.beginCameraRendering += _OnPreRender;
		}
	}

	private void _OnPreRender(ScriptableRenderContext context, Camera cam)
	{
		if (cam == camera)
		{
			OnPreRenderEvent?.Invoke();
		}
	}
}
