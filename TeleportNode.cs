using System.Collections;
using GorillaLocomotion;
using GorillaTagScripts;
using UnityEngine;

public class TeleportNode : GorillaTriggerBox
{
	[SerializeField]
	private XSceneRef teleportFromRef;

	[SerializeField]
	private XSceneRef teleportToRef;

	[SerializeField]
	private bool seamless = true;

	[SerializeField]
	private bool keepVelocity = true;

	[SerializeField]
	private bool subsOnly;

	private float teleportTime;

	public override void OnBoxTriggered()
	{
		if ((subsOnly && !SubscriptionManager.IsLocalSubscribed()) || Time.time - teleportTime < 0.1f)
		{
			return;
		}
		base.OnBoxTriggered();
		if (!teleportFromRef.TryResolve(out Transform result))
		{
			Debug.LogError("[TeleportNode] Failed to resolve teleportFromRef.");
			return;
		}
		if (!teleportToRef.TryResolve(out Transform result2))
		{
			Debug.LogError("[TeleportNode] Failed to resolve teleportToRef.");
			return;
		}
		GTPlayer instance = GTPlayer.Instance;
		if (instance == null)
		{
			Debug.LogError("[TeleportNode] GTPlayer.Instance is null.");
			return;
		}
		Physics.SyncTransforms();
		Vector3 position = result2.transform.position;
		if (seamless)
		{
			position = result2.TransformPoint(result.InverseTransformPoint(instance.transform.position));
		}
		Quaternion quaternion = Quaternion.Inverse(result.rotation) * instance.transform.rotation;
		Quaternion rotation = result2.rotation * quaternion;
		StartCoroutine(DelayedTeleport(instance, position, rotation));
		teleportTime = Time.time;
	}

	private IEnumerator DelayedTeleport(GTPlayer p, Vector3 position, Quaternion rotation)
	{
		yield return null;
		p.TeleportTo(position, rotation, keepVelocity, !seamless);
	}
}
