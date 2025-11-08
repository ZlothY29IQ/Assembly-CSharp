using GorillaLocomotion;
using UnityEngine;

public class SIGadgetTapTeleporterDeployable : MonoBehaviour, IGameEntityComponent
{
	public GameEntity gameEntity;

	[SerializeField]
	private Transform destination;

	[SerializeField]
	private Renderer[] identifierColorDisplay;

	[SerializeField]
	private Transform linkDirectionIndicator;

	[SerializeField]
	private Renderer selectionColorDisplay;

	[SerializeField]
	private Material selectionColor1;

	[SerializeField]
	private Material selectionColor2;

	[SerializeField]
	private SoundBankPlayer teleportSoundbank;

	[SerializeField]
	private SIGameEntityStealthVisibility stealth;

	[SerializeField]
	private bool requiresSurfaceTapSinceTeleport;

	private bool maintainVelocity;

	private int selectionId;

	private SIGadgetTapTeleporter _pad;

	private SIGadgetTapTeleporterDeployable linkedPoint;

	private float activateDelay = 0.3f;

	private float activateTime;

	private static float reteleportDelay = 0.3f;

	private static float reteleportTime;

	private Color identifierColor;

	private float timeToDie = -1f;

	private float teleportCheckDistance = 2f;

	private void Awake()
	{
	}

	private void OnEnable()
	{
		activateTime = Time.time + activateDelay;
	}

	private void LateUpdate()
	{
		if (Time.time > timeToDie && gameEntity.IsAuthority())
		{
			if (linkedPoint != null)
			{
				linkedPoint.ClearLink();
			}
			gameEntity.manager.RequestDestroyItem(gameEntity.id);
		}
	}

	public void OnEntityInit()
	{
		BitPackUtils.UnpackIntsFromLong(gameEntity.createData, out selectionId, out var value);
		if ((float)value < 0f)
		{
			timeToDie = float.PositiveInfinity;
		}
		else
		{
			timeToDie = Time.time + (float)value;
		}
		UpdateSelectionDisplay();
	}

	private void UpdateSelectionDisplay()
	{
		if (selectionId == 0)
		{
			selectionColorDisplay.material = selectionColor1;
		}
		else if (selectionId == 1)
		{
			selectionColorDisplay.material = selectionColor2;
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
		if (gameEntity.IsAuthority())
		{
			return;
		}
		BitPackUtils.UnpackIntsFromLong(newState, out var value, out var value2);
		GameEntity gameEntityFromNetId = gameEntity.manager.GetGameEntityFromNetId(value);
		if (gameEntityFromNetId != null)
		{
			SIGadgetTapTeleporter component = gameEntityFromNetId.GetComponent<SIGadgetTapTeleporter>();
			_pad = component;
			identifierColor = _pad.identifierColor;
		}
		GameEntity gameEntityFromNetId2 = gameEntity.manager.GetGameEntityFromNetId(value2);
		if (gameEntityFromNetId2 != null)
		{
			linkedPoint = gameEntityFromNetId2.GetComponent<SIGadgetTapTeleporterDeployable>();
			if (linkedPoint.linkedPoint == null)
			{
				linkedPoint.linkedPoint = this;
				linkedPoint._pad = _pad;
				linkedPoint.identifierColor = identifierColor;
				linkedPoint.UpdateLinkDisplay();
			}
		}
		else
		{
			linkedPoint = null;
		}
		UpdateLinkDisplay();
	}

	public void SetLink(SIGadgetTapTeleporter newPad, SIGadgetTapTeleporterDeployable newLink)
	{
		_pad = newPad;
		linkedPoint = newLink;
		identifierColor = _pad.identifierColor;
		int value = -1;
		if (linkedPoint != null)
		{
			value = linkedPoint.gameEntity.GetNetId();
		}
		gameEntity.RequestState(gameEntity.id, BitPackUtils.PackIntsIntoLong(_pad.gameEntity.GetNetId(), value));
		UpdateLinkDisplay();
		stealth.enabled = _pad.useStealthTeleporters;
		maintainVelocity = _pad.isVelocityPreserved;
	}

	private void ClearLink()
	{
		linkedPoint = null;
		gameEntity.RequestState(gameEntity.id, BitPackUtils.PackIntsIntoLong(_pad.gameEntity.GetNetId(), -1));
		UpdateLinkDisplay();
	}

	private void UpdateLinkDisplay()
	{
		Renderer[] array = identifierColorDisplay;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].material.color = identifierColor;
		}
		if (linkedPoint != null)
		{
			Vector3 vector = linkedPoint.transform.position - base.transform.position;
			linkDirectionIndicator.gameObject.SetActive(value: true);
			linkDirectionIndicator.transform.rotation = Quaternion.LookRotation(base.transform.forward, vector.normalized);
		}
		else
		{
			linkDirectionIndicator.gameObject.SetActive(value: false);
		}
	}

	public void TryTeleport()
	{
		if (activateTime < Time.time && reteleportTime < Time.time && (!requiresSurfaceTapSinceTeleport || GorillaTagger.Instance.hasTappedSurface))
		{
			TeleportToLinked();
		}
	}

	private void ResetRetriggerBlock()
	{
		reteleportTime = Time.time + reteleportDelay;
	}

	private void TeleportToLinked()
	{
		if (linkedPoint == null || !linkedPoint.gameObject.activeSelf)
		{
			return;
		}
		Vector3 position = destination.position;
		if (!(Vector3.Distance(GTPlayer.Instance.transform.position, position) > teleportCheckDistance))
		{
			ResetRetriggerBlock();
			if (requiresSurfaceTapSinceTeleport)
			{
				GorillaTagger.Instance.ResetTappedSurfaceCheck();
			}
			Vector3 position2 = linkedPoint.destination.position;
			Quaternion rotation = GTPlayer.Instance.transform.rotation;
			GTPlayer.Instance.TeleportTo(position2, rotation, maintainVelocity, center: true);
			linkedPoint.teleportSoundbank.Play();
		}
	}
}
