using UnityEngine;
using UnityEngine.UI;

public class FixedScrollbarSize : MonoBehaviour
{
	public ScrollRect ScrollRect;

	public float HorizontalBarSize = 0.2f;

	public float VerticalBarSize = 0.2f;

	private void OnEnable()
	{
		EnforceScrollbarSize();
		CanvasUpdateRegistry.instance.Equals(null);
		Canvas.willRenderCanvases += EnforceScrollbarSize;
	}

	private void OnDisable()
	{
		Canvas.willRenderCanvases -= EnforceScrollbarSize;
	}

	private void EnforceScrollbarSize()
	{
		if ((bool)ScrollRect.horizontalScrollbar && ScrollRect.horizontalScrollbar.size != HorizontalBarSize)
		{
			ScrollRect.horizontalScrollbar.size = HorizontalBarSize;
		}
		if ((bool)ScrollRect.verticalScrollbar && ScrollRect.verticalScrollbar.size != VerticalBarSize)
		{
			ScrollRect.verticalScrollbar.size = VerticalBarSize;
		}
	}
}
