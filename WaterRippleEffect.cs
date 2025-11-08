using GorillaLocomotion.Swimming;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class WaterRippleEffect : MonoBehaviour
{
	[SerializeField]
	private float ripplePlaybackSpeed = 1f;

	[SerializeField]
	private float fadeOutDelay = 0.5f;

	[SerializeField]
	private float fadeOutTime = 1f;

	private string ripplePlaybackSpeedName = "RipplePlaybackSpeed";

	private int ripplePlaybackSpeedHash;

	private float rippleStartTime = -1f;

	private Animator animator;

	private SpriteRenderer renderer;

	private WaterVolume waterVolume;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		renderer = GetComponent<SpriteRenderer>();
		ripplePlaybackSpeedHash = Animator.StringToHash(ripplePlaybackSpeedName);
	}

	public void Destroy()
	{
		waterVolume = null;
		ObjectPools.instance.Destroy(base.gameObject);
	}

	public void PlayEffect(WaterVolume volume = null)
	{
		waterVolume = volume;
		rippleStartTime = Time.time;
		animator.SetFloat(ripplePlaybackSpeedHash, ripplePlaybackSpeed);
		if (waterVolume != null && waterVolume.Parameters != null)
		{
			renderer.color = waterVolume.Parameters.rippleSpriteColor;
		}
		Color color = renderer.color;
		color.a = 1f;
		renderer.color = color;
	}

	private void Update()
	{
		if (waterVolume != null && !waterVolume.isStationary && waterVolume.surfacePlane != null)
		{
			Vector3 vector = Vector3.Dot(base.transform.position - waterVolume.surfacePlane.position, waterVolume.surfacePlane.up) * waterVolume.surfacePlane.up;
			base.transform.position = base.transform.position - vector;
		}
		float num = Mathf.Clamp01((Time.time - rippleStartTime - fadeOutDelay) / fadeOutTime);
		Color color = renderer.color;
		color.a = 1f - num;
		renderer.color = color;
		if (num >= 1f - Mathf.Epsilon)
		{
			Destroy();
		}
	}
}
