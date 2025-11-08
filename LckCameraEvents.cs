using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class LckCameraEvents : MonoBehaviour
{
	[SerializeField]
	private Camera _camera;

	public UnityEvent onPreRender;

	public UnityEvent onPostRender;

	private void OnEnable()
	{
		RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnbeginCameraRendering;
		RenderPipelineManager.endCameraRendering += RenderPipelineManagerOnendCameraRendering;
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnbeginCameraRendering;
		RenderPipelineManager.endCameraRendering -= RenderPipelineManagerOnendCameraRendering;
	}

	private void RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
	{
		if (!(_camera != camera))
		{
			onPreRender?.Invoke();
		}
	}

	private void RenderPipelineManagerOnendCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
	{
		if (!(_camera != camera))
		{
			onPostRender?.Invoke();
		}
	}
}
