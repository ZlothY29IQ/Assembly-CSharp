using GorillaTag.Cosmetics;
using UnityEngine;

public class VoiceLoudnessReactor : MonoBehaviour
{
	private GorillaSpeakerLoudness loudness;

	[SerializeField]
	private VoiceLoudnessReactorBlendShapeTarget[] blendShapeTargets = new VoiceLoudnessReactorBlendShapeTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorTransformTarget[] transformPositionTargets = new VoiceLoudnessReactorTransformTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorTransformRotationTarget[] transformRotationTargets = new VoiceLoudnessReactorTransformRotationTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorTransformTarget[] transformScaleTargets = new VoiceLoudnessReactorTransformTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorParticleSystemTarget[] particleTargets = new VoiceLoudnessReactorParticleSystemTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorGameObjectEnableTarget[] gameObjectEnableTargets = new VoiceLoudnessReactorGameObjectEnableTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorRendererColorTarget[] rendererColorTargets = new VoiceLoudnessReactorRendererColorTarget[0];

	[SerializeField]
	private VoiceLoudnessReactorAnimatorTarget[] animatorTargets = new VoiceLoudnessReactorAnimatorTarget[0];

	[SerializeField]
	private bool smoothLoudnessForContinuousProperties = true;

	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	private bool hasContinuousProperties;

	private float frameLoudness;

	private float frameSmoothedLoudness;

	[Tooltip("If > 0, The rate that the volume gets louder = deltaTime/attack")]
	[SerializeField]
	private float attack;

	[Tooltip("If > 0, The rate that the volume gets quieter = deltaTime/decay")]
	[SerializeField]
	private float decay;

	private void Start()
	{
		for (int i = 0; i < transformPositionTargets.Length; i++)
		{
			transformPositionTargets[i].Initial = transformPositionTargets[i].transform.localPosition;
		}
		for (int j = 0; j < transformScaleTargets.Length; j++)
		{
			transformScaleTargets[j].Initial = transformScaleTargets[j].transform.localScale;
		}
		for (int k = 0; k < transformRotationTargets.Length; k++)
		{
			transformRotationTargets[k].Initial = transformRotationTargets[k].transform.localRotation;
		}
		for (int l = 0; l < particleTargets.Length; l++)
		{
			particleTargets[l].Main = particleTargets[l].particleSystem.main;
			particleTargets[l].InitialSpeed = particleTargets[l].Main.startSpeedMultiplier;
			particleTargets[l].InitialSize = particleTargets[l].Main.startSizeMultiplier;
			particleTargets[l].Emission = particleTargets[l].particleSystem.emission;
			particleTargets[l].InitialRate = particleTargets[l].Emission.rateOverTimeMultiplier;
			particleTargets[l].Main.startSpeedMultiplier = 0f;
			particleTargets[l].Main.startSizeMultiplier = 0f;
			particleTargets[l].Emission.rateOverTimeMultiplier = 0f;
		}
		for (int m = 0; m < gameObjectEnableTargets.Length; m++)
		{
			gameObjectEnableTargets[m].GameObject.SetActive(!gameObjectEnableTargets[m].TurnOnAtThreshhold);
		}
		for (int n = 0; n < rendererColorTargets.Length; n++)
		{
			rendererColorTargets[n].Inititialize();
		}
		hasContinuousProperties = continuousProperties != null && continuousProperties.Count > 0;
	}

	private void OnEnable()
	{
		if (loudness != null)
		{
			return;
		}
		loudness = GetComponentInParent<GorillaSpeakerLoudness>(includeInactive: true);
		if (loudness == null)
		{
			GorillaTagger componentInParent = GetComponentInParent<GorillaTagger>();
			if (componentInParent != null)
			{
				loudness = componentInParent.offlineVRRig.GetComponent<GorillaSpeakerLoudness>();
			}
		}
		if (loudness != null)
		{
			frameLoudness = loudness.Loudness;
			frameSmoothedLoudness = loudness.SmoothedLoudness;
		}
	}

	private void Update()
	{
		if (loudness == null)
		{
			return;
		}
		_ = loudness.Loudness;
		_ = frameLoudness;
		_ = loudness.Loudness;
		_ = frameLoudness;
		if (attack > 0f && loudness.Loudness > frameLoudness)
		{
			frameLoudness = Mathf.MoveTowards(frameLoudness, loudness.Loudness, Time.deltaTime / attack);
		}
		else if (decay > 0f && loudness.Loudness < frameLoudness)
		{
			frameLoudness = Mathf.MoveTowards(frameLoudness, loudness.Loudness, Time.deltaTime / decay);
		}
		else
		{
			frameLoudness = loudness.Loudness;
		}
		if (attack > 0f && loudness.SmoothedLoudness > frameSmoothedLoudness)
		{
			frameSmoothedLoudness = Mathf.MoveTowards(frameLoudness, loudness.SmoothedLoudness, Time.deltaTime * attack);
		}
		else if (decay > 0f && loudness.SmoothedLoudness < frameSmoothedLoudness)
		{
			frameSmoothedLoudness = Mathf.MoveTowards(frameLoudness, loudness.SmoothedLoudness, Time.deltaTime * decay);
		}
		else
		{
			frameSmoothedLoudness = loudness.SmoothedLoudness;
		}
		for (int i = 0; i < blendShapeTargets.Length; i++)
		{
			float t = (blendShapeTargets[i].UseSmoothedLoudness ? frameSmoothedLoudness : frameLoudness);
			blendShapeTargets[i].SkinnedMeshRenderer.SetBlendShapeWeight(blendShapeTargets[i].BlendShapeIndex, Mathf.Lerp(blendShapeTargets[i].minValue, blendShapeTargets[i].maxValue, t));
		}
		for (int j = 0; j < transformPositionTargets.Length; j++)
		{
			float t2 = (transformPositionTargets[j].UseSmoothedLoudness ? frameSmoothedLoudness : frameLoudness) * transformPositionTargets[j].Scale;
			transformPositionTargets[j].transform.localPosition = Vector3.Lerp(transformPositionTargets[j].Initial, transformPositionTargets[j].Max, t2);
		}
		for (int k = 0; k < transformScaleTargets.Length; k++)
		{
			float t3 = (transformScaleTargets[k].UseSmoothedLoudness ? frameSmoothedLoudness : frameLoudness) * transformScaleTargets[k].Scale;
			transformScaleTargets[k].transform.localScale = Vector3.Lerp(transformScaleTargets[k].Initial, transformScaleTargets[k].Max, t3);
		}
		for (int l = 0; l < transformRotationTargets.Length; l++)
		{
			float t4 = (transformRotationTargets[l].UseSmoothedLoudness ? frameSmoothedLoudness : frameLoudness) * transformRotationTargets[l].Scale;
			transformRotationTargets[l].transform.localRotation = Quaternion.Slerp(transformRotationTargets[l].Initial, transformRotationTargets[l].Max, t4);
		}
		for (int m = 0; m < particleTargets.Length; m++)
		{
			float time = (particleTargets[m].UseSmoothedLoudness ? frameSmoothedLoudness : frameLoudness) * particleTargets[m].Scale;
			particleTargets[m].Main.startSpeedMultiplier = particleTargets[m].InitialSpeed * particleTargets[m].speed.Evaluate(time);
			particleTargets[m].Main.startSizeMultiplier = particleTargets[m].InitialSize * particleTargets[m].size.Evaluate(time);
			particleTargets[m].Emission.rateOverTimeMultiplier = particleTargets[m].InitialRate * particleTargets[m].rate.Evaluate(time);
		}
		for (int n = 0; n < gameObjectEnableTargets.Length; n++)
		{
			bool flag = (gameObjectEnableTargets[n].UseSmoothedLoudness ? frameSmoothedLoudness : (frameLoudness * gameObjectEnableTargets[n].Scale)) >= gameObjectEnableTargets[n].Threshold;
			if (!gameObjectEnableTargets[n].TurnOnAtThreshhold)
			{
				flag = !flag;
			}
			if (gameObjectEnableTargets[n].GameObject.activeInHierarchy != flag)
			{
				gameObjectEnableTargets[n].GameObject.SetActive(flag);
			}
		}
		for (int num = 0; num < rendererColorTargets.Length; num++)
		{
			VoiceLoudnessReactorRendererColorTarget voiceLoudnessReactorRendererColorTarget = rendererColorTargets[num];
			float level = (voiceLoudnessReactorRendererColorTarget.useSmoothedLoudness ? frameSmoothedLoudness : (frameLoudness * voiceLoudnessReactorRendererColorTarget.scale));
			voiceLoudnessReactorRendererColorTarget.UpdateMaterialColor(level);
		}
		for (int num2 = 0; num2 < animatorTargets.Length; num2++)
		{
			VoiceLoudnessReactorAnimatorTarget voiceLoudnessReactorAnimatorTarget = animatorTargets[num2];
			float num3 = (voiceLoudnessReactorAnimatorTarget.useSmoothedLoudness ? frameSmoothedLoudness : frameLoudness);
			if (voiceLoudnessReactorAnimatorTarget.animatorSpeedToLoudness < 0f)
			{
				voiceLoudnessReactorAnimatorTarget.animator.speed = Mathf.Max(0f, (1f - num3) * (0f - voiceLoudnessReactorAnimatorTarget.animatorSpeedToLoudness));
			}
			else
			{
				voiceLoudnessReactorAnimatorTarget.animator.speed = Mathf.Max(0f, num3 * voiceLoudnessReactorAnimatorTarget.animatorSpeedToLoudness);
			}
		}
		if (hasContinuousProperties)
		{
			float f = (smoothLoudnessForContinuousProperties ? frameSmoothedLoudness : frameLoudness);
			continuousProperties.ApplyAll(f);
		}
	}
}
