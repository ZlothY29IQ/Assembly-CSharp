public class BuilderRendererPreRender : MonoBehaviourPostTick
{
	public BuilderRenderer builderRenderer;

	private void Awake()
	{
	}

	public override void PostTick()
	{
		if (builderRenderer != null)
		{
			builderRenderer.PreRenderIndirect();
		}
	}
}
