using UnityEngine;

public class GREnemyBossMoonColliderHelper : MonoBehaviour
{
	public bool ResizeOnAwake = true;

	public Vector3 ResizeCollider = new Vector3(1.025f, 1.025f, 1.025f);

	[SerializeField]
	private GREnemyBossMoon boss;

	[SerializeField]
	private GRPlayer localPlayer;

	private float lastTriggered;

	public void Awake()
	{
		if (ResizeOnAwake)
		{
			base.transform.localScale = ResizeCollider;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (!other.gameObject.CompareTag("GorillaPlayer"))
		{
			return;
		}
		VRRig component = other.attachedRigidbody.GetComponent<VRRig>();
		if (component != null && component == VRRigCache.Instance.localRig.Rig && Time.time - lastTriggered > 0.5f)
		{
			if (localPlayer == null)
			{
				localPlayer = VRRig.LocalRig.GetComponent<GRPlayer>();
			}
			lastTriggered = Time.time;
			boss.HitPlayer(localPlayer, useImpulse: true);
			boss.ShockPlayer();
		}
	}
}
