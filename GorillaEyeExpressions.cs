using UnityEngine;

public class GorillaEyeExpressions : MonoBehaviour, IGorillaSliceableSimple
{
	public GameObject targetFace;

	[Space]
	[SerializeField]
	private float screamVolume = 0.2f;

	[SerializeField]
	private float screamDuration = 0.5f;

	[SerializeField]
	private Vector2 ScreamUV = new Vector2(0.8f, 0f);

	private Vector2 BaseUV = Vector3.zero;

	private GorillaSpeakerLoudness loudness;

	private float overrideDuration;

	private Vector2 overrideUV;

	private float timeLastUpdated;

	private float deltaTime;

	private ShaderHashId _BaseMap_ST = "_BaseMap_ST";

	private void Awake()
	{
		loudness = GetComponent<GorillaSpeakerLoudness>();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		timeLastUpdated = Time.time;
		deltaTime = Time.deltaTime;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		deltaTime = Time.time - timeLastUpdated;
		timeLastUpdated = Time.time;
		CheckEyeEffects();
		UpdateEyeExpression();
	}

	private void CheckEyeEffects()
	{
		if (loudness == null)
		{
			loudness = GetComponent<GorillaSpeakerLoudness>();
		}
		if (loudness.IsSpeaking && loudness.Loudness > screamVolume)
		{
			overrideDuration = screamDuration;
			overrideUV = ScreamUV;
		}
		else if (overrideDuration > 0f)
		{
			overrideDuration -= deltaTime;
			if (overrideDuration <= 0f)
			{
				overrideUV = BaseUV;
			}
		}
	}

	private void UpdateEyeExpression()
	{
		targetFace.GetComponent<Renderer>().material.SetVector(_BaseMap_ST, new Vector4(0.5f, 1f, overrideUV.x, overrideUV.y));
	}
}
