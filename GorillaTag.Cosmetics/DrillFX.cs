using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class DrillFX : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem fx;

	[SerializeField]
	private AnimationCurve fxEmissionCurve;

	[SerializeField]
	private float fxMinRadiusScale = 0.01f;

	[Tooltip("Right click menu has custom menu items. Anything starting with \"- \" is custom.")]
	[SerializeField]
	private AudioSource loopAudio;

	[SerializeField]
	private AnimationCurve loopAudioVolumeCurve;

	[Tooltip("Higher value makes it reach the target volume faster.")]
	[SerializeField]
	private float loopAudioVolumeTransitionSpeed = 3f;

	[FormerlySerializedAs("layerMask")]
	[Tooltip("The collision layers the line cast should intersect with")]
	[SerializeField]
	private LayerMask lineCastLayerMask;

	[Tooltip("The position in local space that the line cast starts.")]
	[SerializeField]
	private Vector3 lineCastStart = Vector3.zero;

	[Tooltip("The position in local space that the line cast ends.")]
	[SerializeField]
	private Vector3 lineCastEnd = Vector3.forward;

	private static bool appIsQuitting;

	private static bool appIsQuittingHandlerIsSubscribed;

	private float maxDepth;

	private bool hasFX;

	private ParticleSystem.EmissionModule fxEmissionModule;

	private float fxEmissionMaxRate;

	private ParticleSystem.ShapeModule fxShapeModule;

	private float fxShapeMaxRadius;

	private bool hasAudio;

	private float audioMaxVolume;

	protected void Awake()
	{
		if (!appIsQuittingHandlerIsSubscribed)
		{
			appIsQuittingHandlerIsSubscribed = true;
			Application.quitting += HandleApplicationQuitting;
		}
		hasFX = fx != null;
		if (hasFX)
		{
			fxEmissionModule = fx.emission;
			fxEmissionMaxRate = fxEmissionModule.rateOverTimeMultiplier;
			fxShapeModule = fx.shape;
			fxShapeMaxRadius = fxShapeModule.radius;
		}
		hasAudio = loopAudio != null;
		if (hasAudio)
		{
			audioMaxVolume = loopAudio.volume;
			loopAudio.volume = 0f;
			loopAudio.loop = true;
			loopAudio.GTPlay();
		}
	}

	protected void OnEnable()
	{
		if (!appIsQuitting)
		{
			if (hasFX)
			{
				fxEmissionModule.rateOverTimeMultiplier = 0f;
			}
			if (hasAudio)
			{
				loopAudio.volume = 0f;
				loopAudio.loop = true;
				loopAudio.GTPlay();
			}
			ValidateLineCastPositions();
		}
	}

	protected void OnDisable()
	{
		if (!appIsQuitting)
		{
			if (hasFX)
			{
				fxEmissionModule.rateOverTimeMultiplier = 0f;
			}
			if (hasAudio)
			{
				loopAudio.volume = 0f;
				loopAudio.GTStop();
			}
		}
	}

	protected void LateUpdate()
	{
		if (!appIsQuitting)
		{
			Transform transform = base.transform;
			RaycastHit hitInfo;
			Vector3 position = (Physics.Linecast(transform.TransformPoint(lineCastStart), transform.TransformPoint(lineCastEnd), out hitInfo, lineCastLayerMask, QueryTriggerInteraction.Ignore) ? hitInfo.point : lineCastEnd);
			Vector3 vector = transform.InverseTransformPoint(position);
			float num = Mathf.Clamp01(Vector3.Distance(lineCastStart, vector) / maxDepth);
			if (hasFX)
			{
				fxEmissionModule.rateOverTimeMultiplier = fxEmissionMaxRate * fxEmissionCurve.Evaluate(num);
				fxShapeModule.position = vector;
				fxShapeModule.radius = Mathf.Lerp(fxShapeMaxRadius, fxMinRadiusScale * fxShapeMaxRadius, num);
			}
			if (hasAudio)
			{
				loopAudio.volume = Mathf.MoveTowards(loopAudio.volume, audioMaxVolume * loopAudioVolumeCurve.Evaluate(num), loopAudioVolumeTransitionSpeed * Time.deltaTime);
			}
		}
	}

	private static void HandleApplicationQuitting()
	{
		appIsQuitting = true;
	}

	private bool ValidateLineCastPositions()
	{
		maxDepth = Vector3.Distance(lineCastStart, lineCastEnd);
		if (maxDepth > float.Epsilon)
		{
			return true;
		}
		if (Application.isPlaying)
		{
			Debug.Log("DrillFX: lineCastStart and End are too close together. Disabling component.", this);
			base.enabled = false;
		}
		return false;
	}
}
