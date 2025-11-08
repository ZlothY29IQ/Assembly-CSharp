using UnityEngine;

namespace GorillaTag.Cosmetics;

public class UpdateBlendShapeCosmetic : MonoBehaviour
{
	[Tooltip("The SkinnedMeshRenderer whose BlendShape weight will be updated. This must reference a mesh that has BlendShapes defined in its import settings.")]
	[SerializeField]
	private SkinnedMeshRenderer skinnedMeshRenderer;

	[Tooltip("Maximum blend shape weight applied when fully blended. Usually 100 for standard Unity BlendShapes.")]
	public float maxBlendShapeWeight = 100f;

	[Tooltip("Index of the BlendShape to control. You can find this index in the SkinnedMeshRenderer inspector under 'BlendShapes'.")]
	[SerializeField]
	private int blendShapeIndex;

	[Tooltip("Speed at which the BlendShape transitions toward its target weight. Higher values make blending more responsive, lower values make it smoother.")]
	[SerializeField]
	private float blendSpeed = 10f;

	[Tooltip("Initial BlendShape weight set when the component awakens. Useful for setting a default deformation state.")]
	[SerializeField]
	private float blendStartWeight;

	[Tooltip("If enabled, inverts the incoming blend value (e.g. 0 → 1, 0.2 → 0.8). Useful when an input should drive the opposite direction of deformation.")]
	[SerializeField]
	private bool invertPassedBlend;

	private float targetWeight;

	private float currentWeight;

	private void Awake()
	{
		targetWeight = blendStartWeight;
		currentWeight = 0f;
	}

	private void Update()
	{
		currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);
		skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, currentWeight);
	}

	public void SetBlendValue(bool leftHand, float value)
	{
		targetWeight = Mathf.Clamp01(invertPassedBlend ? (1f - value) : value) * maxBlendShapeWeight;
	}

	public void SetBlendValue(float value)
	{
		targetWeight = Mathf.Clamp01(invertPassedBlend ? (1f - value) : value) * maxBlendShapeWeight;
	}

	public void FullyBlend()
	{
		targetWeight = maxBlendShapeWeight;
	}

	public void ResetBlend()
	{
		targetWeight = 0f;
	}

	public float GetBlendValue()
	{
		return skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
	}
}
