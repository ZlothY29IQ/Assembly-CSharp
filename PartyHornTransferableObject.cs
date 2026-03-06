using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class PartyHornTransferableObject : TransferrableObject
{
	private enum PartyHornState
	{
		None = 1,
		CoolingDown
	}

	[Tooltip("This GameObject will activate when held to any gorilla's mouth.")]
	public GameObject effectsGameObject;

	public float cooldown = 2f;

	public float mouthPieceZOffset = -0.18f;

	public float mouthPieceRadius = 0.05f;

	public Transform mouthPiece;

	public bool soundActivated;

	public UnityEvent OnCooldownStart;

	public UnityEvent OnCooldownReset;

	private float cooldownRemaining;

	private PartyHornState partyHornStateLastFrame;

	private bool localWasActivated;

	internal override void OnEnable()
	{
		base.OnEnable();
		InitToDefault();
	}

	internal override void OnDisable()
	{
		base.OnDisable();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		InitToDefault();
	}

	protected Vector3 CalcMouthPiecePos()
	{
		if (!mouthPiece)
		{
			return base.transform.position + mouthPieceZOffset * base.transform.forward;
		}
		return mouthPiece.position;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (!InHand() || itemState != ItemStates.State0 || !GorillaParent.hasInstance)
		{
			return;
		}
		_ = base.transform;
		Vector3 vector = CalcMouthPiecePos();
		float num = mouthPieceRadius * mouthPieceRadius * GTPlayer.Instance.scale * GTPlayer.Instance.scale;
		bool flag = (GorillaTagger.Instance.offlineVRRig.GetMouthPosition() - vector).sqrMagnitude < num;
		if (soundActivated && PhotonNetwork.InRoom)
		{
			int num2;
			if (flag)
			{
				GorillaTagger instance = GorillaTagger.Instance;
				num2 = (((object)instance != null && instance.myRecorder?.IsCurrentlyTransmitting == true) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			flag = (byte)num2 != 0;
		}
		for (int i = 0; i < VRRigCache.ActiveRigContainers.Count; i++)
		{
			VRRig rig = VRRigCache.ActiveRigContainers[i].Rig;
			if (flag)
			{
				break;
			}
			flag = (rig.GetMouthPosition() - vector).sqrMagnitude < num;
			if (soundActivated)
			{
				int num3;
				if (flag)
				{
					RigContainer rigContainer = rig.rigContainer;
					num3 = (((object)rigContainer != null && rigContainer.Voice?.IsSpeaking == true) ? 1 : 0);
				}
				else
				{
					num3 = 0;
				}
				flag = (byte)num3 != 0;
			}
		}
		itemState = (flag ? ItemStates.State1 : itemState);
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (ItemStates.State1 != itemState)
		{
			return;
		}
		if (!localWasActivated)
		{
			if ((bool)effectsGameObject)
			{
				effectsGameObject.SetActive(value: true);
			}
			cooldownRemaining = cooldown;
			localWasActivated = true;
			OnCooldownStart?.Invoke();
		}
		cooldownRemaining -= Time.deltaTime;
		if (cooldownRemaining <= 0f)
		{
			InitToDefault();
		}
	}

	private void InitToDefault()
	{
		itemState = ItemStates.State0;
		if ((bool)effectsGameObject)
		{
			effectsGameObject.SetActive(value: false);
		}
		cooldownRemaining = cooldown;
		localWasActivated = false;
		OnCooldownReset?.Invoke();
	}
}
