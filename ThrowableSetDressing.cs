using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkView))]
public class ThrowableSetDressing : TransferrableObject
{
	public float respawnTimerDuration;

	[Tooltip("set this only if this set dressing is using as an ingredient for the magic cauldron - Halloween")]
	public MagicIngredientType IngredientTypeSO;

	private float _respawnTimestamp;

	[SerializeField]
	private CapsuleCollider capsuleCollider;

	private NetworkView netView;

	private Vector3 respawnAtPos;

	private Quaternion respawnAtRot;

	private Coroutine respawnTimer;

	public bool inInitialPose { get; private set; } = true;

	public override bool ShouldBeKinematic()
	{
		if (!inInitialPose)
		{
			return base.ShouldBeKinematic();
		}
		return true;
	}

	protected override void Awake()
	{
		base.Awake();
		netView = GetComponent<NetworkView>();
	}

	protected override void Start()
	{
		base.Start();
		respawnAtPos = base.transform.position;
		respawnAtRot = base.transform.rotation;
		currentState = PositionState.Dropped;
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		inInitialPose = false;
		StopRespawnTimer();
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		StartRespawnTimer();
		return true;
	}

	public override void DropItem()
	{
		base.DropItem();
		StartRespawnTimer();
	}

	private void StopRespawnTimer()
	{
		if (respawnTimer != null)
		{
			StopCoroutine(respawnTimer);
			respawnTimer = null;
		}
	}

	public void SetWillTeleport()
	{
		worldShareableInstance.SetWillTeleport();
	}

	public void StartRespawnTimer(float overrideTimer = -1f)
	{
		float timerDuration = ((overrideTimer != -1f) ? overrideTimer : respawnTimerDuration);
		StopRespawnTimer();
		if (respawnTimerDuration != 0f && (!netView.IsValid || netView.IsMine))
		{
			respawnTimer = StartCoroutine(RespawnTimerCoroutine(timerDuration));
		}
	}

	private IEnumerator RespawnTimerCoroutine(float timerDuration)
	{
		yield return new WaitForSeconds(timerDuration);
		if (!InHand())
		{
			SetWillTeleport();
			base.transform.position = respawnAtPos;
			base.transform.rotation = respawnAtRot;
			inInitialPose = true;
			rigidbodyInstance.isKinematic = true;
		}
	}
}
