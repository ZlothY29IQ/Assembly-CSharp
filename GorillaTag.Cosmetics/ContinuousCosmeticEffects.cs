using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ContinuousCosmeticEffects : MonoBehaviour
{
	[FormerlySerializedAs("properties")]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	public void ApplyAll(float f)
	{
		continuousProperties.ApplyAll(f);
	}
}
