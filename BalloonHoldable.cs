using GorillaExtensions;
using GorillaLocomotion.Swimming;
using GorillaTag;
using Photon.Pun;
using UnityEngine;

public class BalloonHoldable : TransferrableObject, IFXContext
{
	private enum BalloonStates
	{
		Normal,
		Pop,
		Waiting,
		WaitForOwnershipTransfer,
		WaitForReDock,
		Refilling,
		Returning
	}

	private ITetheredObjectBehavior balloonDynamics;

	[SerializeField]
	private Renderer mesh;

	private LineRenderer lineRenderer;

	private Rigidbody rb;

	private NetPlayer originalOwner;

	public GameObject balloonPopFXPrefab;

	public Color balloonPopFXColor;

	private float timer;

	public float scaleTimerLength = 2f;

	public float poppedTimerLength = 2.5f;

	public float beginScale = 0.1f;

	public float bopSpeed = 1f;

	private Vector3 fullyInflatedScale;

	public AudioSource balloonBopSource;

	public AudioSource balloonInflatSource;

	private Vector3 forceAppliedAsRemote;

	private Vector3 collisionPtAsRemote;

	private WaterVolume waterVolume;

	[DebugReadout]
	private BalloonStates balloonState;

	private float returnTimer;

	[SerializeField]
	private float maxDistanceFromOwner;

	public float lastOwnershipRequest;

	[SerializeField]
	private bool disableCollisionHandling;

	[SerializeField]
	private bool disableRelease;

	FXSystemSettings IFXContext.settings => ownerRig.fxSettings;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		balloonDynamics = GetComponent<ITetheredObjectBehavior>();
		if (mesh == null)
		{
			mesh = GetComponent<Renderer>();
		}
		lineRenderer = GetComponent<LineRenderer>();
		itemState = (ItemStates)0;
		rb = GetComponent<Rigidbody>();
	}

	protected override void Start()
	{
		base.Start();
		EnableDynamics(enable: false, collider: false);
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		EnableDynamics(enable: false, collider: false);
		mesh.enabled = true;
		lineRenderer.enabled = false;
		if (NetworkSystem.Instance.InRoom)
		{
			if (worldShareableInstance != null)
			{
				return;
			}
			SpawnTransferableObjectViews();
		}
		if (InHand())
		{
			Grab();
		}
		else if (Dropped())
		{
			Release();
		}
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		if (!(worldShareableInstance != null))
		{
			SpawnTransferableObjectViews();
		}
	}

	private bool ShouldSimulate()
	{
		if (!Attached())
		{
			return balloonState == BalloonStates.Normal;
		}
		return false;
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		lineRenderer.enabled = false;
		EnableDynamics(enable: false, collider: false);
	}

	public override void PreDisable()
	{
		originalOwner = null;
		base.PreDisable();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		balloonState = BalloonStates.Normal;
		base.transform.localScale = Vector3.one;
	}

	protected override void OnWorldShareableItemSpawn()
	{
		WorldShareableItem worldShareableItem = worldShareableInstance;
		if (worldShareableItem != null)
		{
			worldShareableItem.rpcCallBack = PopBalloonRemote;
			worldShareableItem.onOwnerChangeCb = OnOwnerChangeCb;
			worldShareableItem.EnableRemoteSync = ShouldSimulate();
		}
		originalOwner = worldShareableItem.target.owner;
	}

	public override void ResetToHome()
	{
		if (IsLocalObject() && worldShareableInstance != null && !worldShareableInstance.guard.isTrulyMine)
		{
			PhotonView photonView = PhotonView.Get(worldShareableInstance);
			if (photonView != null)
			{
				photonView.RPC("RPCWorldShareable", RpcTarget.Others);
			}
			worldShareableInstance.guard.RequestOwnershipImmediatelyWithGuaranteedAuthority();
		}
		PopBalloon();
		balloonState = BalloonStates.WaitForReDock;
		base.ResetToHome();
	}

	protected override void PlayDestroyedOrDisabledEffect()
	{
		base.PlayDestroyedOrDisabledEffect();
		PlayPopBalloonFX();
	}

	protected override void OnItemDestroyedOrDisabled()
	{
		PlayPopBalloonFX();
		if (balloonDynamics != null)
		{
			balloonDynamics.ReParent();
		}
		base.transform.parent = DefaultAnchor();
		base.OnItemDestroyedOrDisabled();
	}

	private void PlayPopBalloonFX()
	{
		FXSystem.PlayFXForRig(FXType.BalloonPop, this);
	}

	private void EnableDynamics(bool enable, bool collider, bool forceKinematicOn = false)
	{
		bool kinematic = false;
		if (forceKinematicOn)
		{
			kinematic = true;
		}
		else if (NetworkSystem.Instance.InRoom && worldShareableInstance != null)
		{
			PhotonView photonView = PhotonView.Get(worldShareableInstance.gameObject);
			if (photonView != null && !photonView.IsMine)
			{
				kinematic = true;
			}
		}
		if (balloonDynamics != null)
		{
			balloonDynamics.EnableDynamics(enable, collider, kinematic);
		}
	}

	private void PopBalloon()
	{
		PlayPopBalloonFX();
		EnableDynamics(enable: false, collider: false);
		mesh.enabled = false;
		lineRenderer.enabled = false;
		if (gripInteractor != null)
		{
			gripInteractor.gameObject.SetActive(value: false);
		}
		if ((object.Equals(originalOwner, PhotonNetwork.LocalPlayer) || !NetworkSystem.Instance.InRoom) && NetworkSystem.Instance.InRoom && worldShareableInstance != null && !worldShareableInstance.guard.isTrulyMine)
		{
			worldShareableInstance.guard.RequestOwnershipImmediatelyWithGuaranteedAuthority();
		}
		if (balloonDynamics != null)
		{
			balloonDynamics.ReParent();
			EnableDynamics(enable: false, collider: false);
		}
		if (IsMyItem())
		{
			if (InLeftHand())
			{
				EquipmentInteractor.instance.ReleaseLeftHand();
			}
			if (InRightHand())
			{
				EquipmentInteractor.instance.ReleaseRightHand();
			}
		}
	}

	public void PopBalloonRemote()
	{
		if (ShouldSimulate())
		{
			balloonState = BalloonStates.Pop;
		}
	}

	public void OnOwnerChangeCb(NetPlayer newOwner, NetPlayer prevOwner)
	{
	}

	public override void OnOwnershipTransferred(NetPlayer newOwner, NetPlayer prevOwner)
	{
		base.OnOwnershipTransferred(newOwner, prevOwner);
		if (object.Equals(prevOwner, NetworkSystem.Instance.LocalPlayer) && newOwner == null)
		{
			return;
		}
		if (!object.Equals(newOwner, NetworkSystem.Instance.LocalPlayer))
		{
			EnableDynamics(enable: false, collider: true, forceKinematicOn: true);
			return;
		}
		if (ShouldSimulate() && balloonDynamics != null)
		{
			balloonDynamics.EnableDynamics(enable: true, collider: true, kinematic: false);
		}
		if ((bool)rb)
		{
			if (!rb.isKinematic)
			{
				rb.AddForceAtPosition(forceAppliedAsRemote * rb.mass, collisionPtAsRemote, ForceMode.Impulse);
			}
			forceAppliedAsRemote = Vector3.zero;
			collisionPtAsRemote = Vector3.zero;
		}
	}

	private void OwnerPopBalloon()
	{
		if (worldShareableInstance != null)
		{
			PhotonView photonView = PhotonView.Get(worldShareableInstance);
			if (photonView != null)
			{
				photonView.RPC("RPCWorldShareable", RpcTarget.Others);
			}
		}
		balloonState = BalloonStates.Pop;
	}

	private void RunLocalPopSM()
	{
		switch (balloonState)
		{
		case BalloonStates.Pop:
			timer = Time.time;
			PopBalloon();
			balloonState = BalloonStates.WaitForOwnershipTransfer;
			lastOwnershipRequest = Time.time;
			break;
		case BalloonStates.WaitForOwnershipTransfer:
			if (!NetworkSystem.Instance.InRoom)
			{
				balloonState = BalloonStates.WaitForReDock;
				ReDock();
			}
			else if (worldShareableInstance != null)
			{
				WorldShareableItem worldShareableItem = worldShareableInstance;
				NetPlayer owner = worldShareableItem.Owner;
				if (worldShareableItem != null && owner == originalOwner)
				{
					balloonState = BalloonStates.WaitForReDock;
					ReDock();
				}
				if (IsLocalObject() && lastOwnershipRequest + 5f < Time.time)
				{
					worldShareableInstance.guard.RequestOwnershipImmediatelyWithGuaranteedAuthority();
					lastOwnershipRequest = Time.time;
				}
			}
			break;
		case BalloonStates.WaitForReDock:
			if (Attached())
			{
				fullyInflatedScale = base.transform.localScale;
				ReDock();
				balloonState = BalloonStates.Waiting;
			}
			break;
		case BalloonStates.Waiting:
			if (Time.time - timer >= poppedTimerLength)
			{
				timer = Time.time;
				mesh.enabled = true;
				balloonInflatSource.GTPlay();
				balloonState = BalloonStates.Refilling;
			}
			else
			{
				base.transform.localScale = new Vector3(beginScale, beginScale, beginScale);
			}
			break;
		case BalloonStates.Refilling:
		{
			float num = Time.time - timer;
			if (num >= scaleTimerLength)
			{
				base.transform.localScale = fullyInflatedScale;
				balloonState = BalloonStates.Normal;
				if (gripInteractor != null)
				{
					gripInteractor.gameObject.SetActive(value: true);
				}
			}
			num = Mathf.Clamp01(num / scaleTimerLength);
			float num2 = Mathf.Lerp(beginScale, 1f, num);
			base.transform.localScale = fullyInflatedScale * num2;
			break;
		}
		case BalloonStates.Returning:
			if (balloonDynamics.ReturnStep())
			{
				balloonState = BalloonStates.Normal;
				ReDock();
			}
			break;
		case BalloonStates.Normal:
			break;
		}
	}

	protected override void OnStateChanged()
	{
		if (InHand())
		{
			Grab();
		}
		else if (Dropped())
		{
			Release();
		}
		else if (OnShoulder())
		{
			if (balloonDynamics != null && balloonDynamics.IsEnabled())
			{
				EnableDynamics(enable: false, collider: false);
			}
			lineRenderer.enabled = false;
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (Time.frameCount == enabledOnFrame)
		{
			OnStateChanged();
		}
		if (InHand() && detatchOnGrab)
		{
			float num = ((targetRig != null) ? targetRig.transform.localScale.x : 1f);
			base.transform.localScale = Vector3.one * num;
		}
		if (Dropped() && balloonState == BalloonStates.Normal && maxDistanceFromOwner > 0f && (!NetworkSystem.Instance.InRoom || originalOwner.IsLocal) && (VRRig.LocalRig.transform.position - base.transform.position).IsLongerThan(maxDistanceFromOwner * base.transform.localScale.x))
		{
			OwnerPopBalloon();
		}
		if ((object)worldShareableInstance != null && !worldShareableInstance.guard.isMine)
		{
			worldShareableInstance.EnableRemoteSync = ShouldSimulate();
		}
		if (balloonState != BalloonStates.Normal)
		{
			RunLocalPopSM();
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
	}

	private void Grab()
	{
		if (balloonDynamics != null)
		{
			if (detatchOnGrab)
			{
				float num = ((targetRig != null) ? targetRig.transform.localScale.x : 1f);
				base.transform.localScale = Vector3.one * num;
				EnableDynamics(enable: true, collider: true);
				balloonDynamics.EnableDistanceConstraints(v: true, num);
				lineRenderer.enabled = true;
			}
			else
			{
				base.transform.localScale = Vector3.one;
			}
		}
	}

	private void Release()
	{
		if (disableRelease)
		{
			balloonState = BalloonStates.Returning;
		}
		else if (balloonDynamics != null)
		{
			float num = ((targetRig != null) ? targetRig.transform.localScale.x : 1f);
			base.transform.localScale = Vector3.one * num;
			EnableDynamics(enable: true, collider: true);
			balloonDynamics.EnableDistanceConstraints(v: false, num);
			lineRenderer.enabled = false;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (!ShouldSimulate())
		{
			return;
		}
		Vector3 force = Vector3.zero;
		Vector3 collisionPt = Vector3.zero;
		bool transferOwnership = false;
		if (balloonDynamics != null)
		{
			balloonDynamics.TriggerEnter(other, ref force, ref collisionPt, ref transferOwnership);
		}
		if (!NetworkSystem.Instance.InRoom || worldShareableInstance == null || !transferOwnership)
		{
			return;
		}
		RequestableOwnershipGuard component = PhotonView.Get(worldShareableInstance.gameObject).GetComponent<RequestableOwnershipGuard>();
		if (!component.isTrulyMine)
		{
			if (force.magnitude > forceAppliedAsRemote.magnitude)
			{
				forceAppliedAsRemote = force;
				collisionPtAsRemote = collisionPt;
			}
			component.RequestOwnershipImmediately(delegate
			{
			});
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (!ShouldSimulate() || disableCollisionHandling)
		{
			return;
		}
		balloonBopSource.GTPlay();
		if (collision.gameObject.IsOnLayer(UnityLayer.GorillaThrowable))
		{
			if (!NetworkSystem.Instance.InRoom)
			{
				OwnerPopBalloon();
			}
			else if (!(worldShareableInstance == null) && PhotonView.Get(worldShareableInstance.gameObject).IsMine)
			{
				OwnerPopBalloon();
			}
		}
	}

	void IFXContext.OnPlayFX()
	{
		GameObject obj = ObjectPools.instance.Instantiate(balloonPopFXPrefab);
		obj.transform.SetPositionAndRotation(base.transform.position, base.transform.rotation);
		GorillaColorizableBase componentInChildren = obj.GetComponentInChildren<GorillaColorizableBase>();
		if (componentInChildren != null)
		{
			componentInChildren.SetColor(balloonPopFXColor);
		}
	}
}
