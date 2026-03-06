using UnityEngine;

[RequireComponent(typeof(GameTriggerInteractable))]
public class SIGadgetPumpBlaster : MonoBehaviour, SIGadgetBlasterType
{
	public SIGadgetBlasterProjectile projectilePrefab;

	public AudioClip idleClip;

	public AudioClip cooldownClip;

	public float idleVolume;

	public float cooldownVolume;

	public AudioClip firingClip;

	public float firingVolume;

	public ParticleSystem fireFX;

	public Transform pumpHandlePosition;

	public Transform pumpFullyClosed;

	public Transform pumpFullyOpen;

	private GameTriggerInteractable triggerInteractable;

	private SIGadgetBlaster blaster;

	private Transform pumpingTransform;

	public float currentPumpChargeAmount;

	public float maxPumpCharge = 1f;

	public float remotePumpChargePerSecond = 2f;

	public float maxPumpDiff = 0.5f;

	private float chargePerPump = 1f;

	private bool pumpFullyOpened;

	private float pumpThresholdPercent = 0.1f;

	private float strokeLength;

	private bool CheckInput()
	{
		return blaster.CheckInput();
	}

	private void OnEnable()
	{
		blaster = GetComponent<SIGadgetBlaster>();
		triggerInteractable = GetComponent<GameTriggerInteractable>();
		strokeLength = (pumpFullyClosed.position - pumpFullyOpen.position).magnitude;
	}

	public void OnUpdateAuthority(float dt)
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			if (triggerInteractable.triggerInteractionActive)
			{
				blaster.SetStateAuthority(SIGadgetBlasterState.Pumping);
			}
			else if (CheckInput() && currentPumpChargeAmount > 0f)
			{
				AttemptFireProjectile(blaster.NextFireId(), currentPumpChargeAmount, blaster.firingPosition.position, blaster.firingPosition.rotation);
			}
			break;
		case SIGadgetBlasterState.Pumping:
		{
			if (!triggerInteractable.triggerInteractionActive)
			{
				blaster.SetStateAuthority(SIGadgetBlasterState.Idle);
			}
			Vector3 vector = pumpFullyOpen.position - pumpFullyClosed.position;
			Vector3 vector2 = pumpingTransform.position - pumpFullyClosed.position;
			if (Vector3.Dot(vector, vector2) < 0f)
			{
				vector2 = Vector3.zero;
			}
			Vector3 vector3 = Vector3.Project(vector2, vector);
			pumpHandlePosition.position = pumpFullyClosed.position + vector.normalized * Mathf.Clamp(vector3.magnitude, 0f, vector.magnitude);
			if (!pumpFullyOpened && vector3.magnitude > (1f - pumpThresholdPercent) * strokeLength)
			{
				pumpFullyOpened = true;
			}
			else if ((bool)pumpFullyOpen && vector3.magnitude < pumpThresholdPercent * strokeLength)
			{
				pumpFullyOpened = false;
				currentPumpChargeAmount = Mathf.Min(currentPumpChargeAmount + chargePerPump, maxPumpCharge);
			}
			if (CheckInput() && currentPumpChargeAmount > 0f)
			{
				AttemptFireProjectile(blaster.NextFireId(), currentPumpChargeAmount, blaster.firingPosition.position, blaster.firingPosition.rotation);
			}
			break;
		}
		}
	}

	public void OnUpdateRemote(float dt)
	{
		SIGadgetBlasterState currentState = blaster.currentState;
		if (currentState != SIGadgetBlasterState.Idle && currentState == SIGadgetBlasterState.Pumping)
		{
			Vector3 vector = pumpFullyOpen.position - pumpFullyClosed.position;
			Vector3 vector2 = pumpingTransform.position - pumpFullyClosed.position;
			if (Vector3.Dot(vector, vector2) < 0f)
			{
				vector2 = Vector3.zero;
			}
			Vector3 vector3 = Vector3.Project(vector2, vector);
			pumpHandlePosition.position = pumpFullyClosed.position + vector.normalized * Mathf.Clamp(vector3.magnitude, 0f, vector.magnitude);
			currentPumpChargeAmount = Mathf.Min(maxPumpCharge, currentPumpChargeAmount + Time.deltaTime * remotePumpChargePerSecond);
		}
	}

	public void SetStateShared()
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			blaster.blasterSource.clip = idleClip;
			blaster.blasterSource.volume = idleVolume;
			pumpingTransform = null;
			break;
		case SIGadgetBlasterState.Pumping:
		{
			GameEntity gameEntity = blaster.gameEntity;
			if (GamePlayer.TryGetGamePlayer(gameEntity.AttachedPlayerActorNr, out var out_gamePlayer))
			{
				pumpingTransform = gameEntity.EquippedHandedness switch
				{
					EHandedness.Left => out_gamePlayer.rightHand, 
					EHandedness.Right => out_gamePlayer.leftHand, 
					_ => pumpingTransform, 
				};
			}
			break;
		}
		}
	}

	public void AttemptFireProjectile(int fireId, float pumpChargeAmount, Vector3 position, Quaternion rotation)
	{
		if (!(pumpChargeAmount <= 0f) && !(pumpChargeAmount - maxPumpDiff > currentPumpChargeAmount) && blaster.projectileCount <= blaster.maxProjectileCount)
		{
			if (blaster.LocalEquippedOrActivated)
			{
				blaster.SendClientToClientRPC(0, new object[3] { fireId, position, rotation });
			}
			currentPumpChargeAmount = Mathf.Min(maxPumpCharge, pumpChargeAmount);
			blaster.firingSource.time = 0f;
			blaster.firingSource.Play();
			blaster.firingSource.loop = false;
			blaster.InstantiateProjectile(projectilePrefab, position, rotation, fireId);
			currentPumpChargeAmount = 0f;
		}
	}

	public void NetworkFireProjectile(object[] data)
	{
		if (data != null && data.Length == 4 && GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) && GameEntityManager.ValidateDataType<float>(data[1], out var dataAsType2) && GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType3) && GameEntityManager.ValidateDataType<Quaternion>(data[3], out var dataAsType4))
		{
			AttemptFireProjectile(dataAsType, dataAsType2, dataAsType3, dataAsType4);
		}
	}

	public void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
	}
}
