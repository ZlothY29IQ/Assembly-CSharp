using GorillaExtensions;
using UnityEngine;

public class StageMicrophone : MonoBehaviour
{
	public static StageMicrophone Instance;

	[SerializeField]
	private float PickupRadius;

	[SerializeField]
	private float AmplifiedSpatialBlend;

	private void Awake()
	{
		Instance = this;
	}

	public bool IsPlayerAmplified(VRRig player)
	{
		return (player.GetMouthPosition() - base.transform.position).IsShorterThan(PickupRadius);
	}

	public float GetPlayerSpatialBlend(VRRig player)
	{
		if (!IsPlayerAmplified(player))
		{
			return 0.9f;
		}
		return AmplifiedSpatialBlend;
	}
}
