using UnityEngine;

public class XRaySkeleton : SyncToPlayerColor, IGorillaSimpleBackgroundWorker
{
	public SkinnedMeshRenderer renderer;

	public Vector2 baseValueMinMax = new Vector2(0.69f, 1f);

	public Material[] tagMaterials = new Material[0];

	private int _lastMatIndex;

	private Material[] mats;

	private int currentIndex = 1;

	private static readonly ShaderHashId _BaseColor = "_BaseColor";

	private static readonly ShaderHashId _EmissionColor = "_EmissionColor";

	protected override void Awake()
	{
		base.Awake();
		target = renderer.material;
		mats = rig.materialsToChangeTo;
		tagMaterials = new Material[mats.Length];
		tagMaterials[0] = new Material(target);
		GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
	}

	public void SimpleWork()
	{
		if (currentIndex >= 0 && currentIndex < mats.Length)
		{
			Material material = new Material(mats[currentIndex]);
			tagMaterials[currentIndex] = material;
			currentIndex++;
			GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
		}
	}

	public void SetMaterialIndex(int index)
	{
		renderer.sharedMaterial = tagMaterials[index];
		_lastMatIndex = index;
	}

	private void Setup()
	{
		colorPropertiesToSync = new ShaderHashId[2] { _BaseColor, _EmissionColor };
	}

	public override void UpdateColor(Color color)
	{
		if (_lastMatIndex == 0)
		{
			Material material = tagMaterials[0];
			Color.RGBToHSV(color, out var H, out var S, out var V);
			Color value = Color.HSVToRGB(H, S, Mathf.Clamp(V, baseValueMinMax.x, baseValueMinMax.y));
			material.SetColor(_BaseColor, value);
			Color.RGBToHSV(color, out var H2, out var _, out var _);
			Color color2 = Color.HSVToRGB(H2, 0.82f, 0.9f, hdr: true);
			color2 = new Color(color2.r * 1.4f, color2.g * 1.4f, color2.b * 1.4f);
			material.SetColor(_EmissionColor, ColorUtils.ComposeHDR(new Color32(36, 191, 136, byte.MaxValue), 2f));
			renderer.sharedMaterial = material;
		}
	}
}
