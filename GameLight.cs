using UnityEngine;

public class GameLight : MonoBehaviour
{
	public Light light;

	public bool negativeLight;

	public bool isHighPriorityPlayerLight;

	public Vector3 cachedPosition;

	public Vector4 cachedColorAndIntensity;

	public int lightId;

	public int intensityMult = 1;

	private bool initialized;

	public float InitialIntensity { get; private set; }

	public void Awake()
	{
		intensityMult = 1;
	}

	private void OnEnable()
	{
		if (initialized)
		{
			lightId = GameLightingManager.instance.AddGameLight(this);
		}
	}

	private void Start()
	{
		lightId = GameLightingManager.instance.AddGameLight(this);
		initialized = true;
	}

	private void OnDisable()
	{
		if (initialized)
		{
			GameLightingManager.instance.RemoveGameLight(this);
		}
	}
}
