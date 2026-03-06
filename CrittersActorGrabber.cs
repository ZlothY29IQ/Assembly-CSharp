using System;
using System.Collections;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

[DefaultExecutionOrder(9999)]
public class CrittersActorGrabber : MonoBehaviour
{
	public bool isGrabbing;

	public Collider[] colliders = new Collider[50];

	public bool isLeft;

	public float grabRadius = 0.05f;

	public float grabBreakRadius = 0.15f;

	private float grabDetachFromBagDist = 0.05f;

	public Transform transformToFollow;

	public GorillaVelocityEstimator estimator;

	public CrittersGrabber grabber;

	public float vibrationStartDistance;

	public float vibrationEndDistance;

	public CrittersActorGrabber otherHand;

	private bool isHandGrabbingDisabled;

	private float grabDuration = 0.3f;

	private float remainingGrabDuration;

	private bool playingHaptics;

	private AudioClip hapticsClip;

	private float hapticsStrength;

	private float hapticsLength;

	private Coroutine haptics;

	public CapsuleCollider triggerCollider;

	private Rigidbody rb;

	private CrittersActor validGrabTarget;

	private CrittersActor lastHover;

	private Vector3 localGrabOffset;

	private CrittersActor queuedGrab;

	private Vector3 queuedRelativeGrabOffset;

	private Quaternion queuedRelativeGrabRotation;

	public List<CrittersActor> actorsStillPresent;

	private void Awake()
	{
		if (grabber == null)
		{
			grabber = GetComponent<CrittersGrabber>();
		}
		vibrationStartDistance *= vibrationStartDistance;
		vibrationEndDistance *= vibrationEndDistance;
		rb = GetComponent<Rigidbody>();
		CrittersGrabberSharedData.AddActorGrabber(this);
		actorsStillPresent = new List<CrittersActor>();
	}

	private void LateUpdate()
	{
		if (CrittersManager.instance == null || !CrittersManager.instance.LocalInZone)
		{
			return;
		}
		if (isLeft)
		{
			NewJointMethod();
		}
		if ((grabber == null || !grabber.gameObject.activeSelf || grabber.rigPlayerId != PhotonNetwork.LocalPlayer.ActorNumber) && CrittersManager.instance.rigSetupByRig.TryGetValue(GorillaTagger.Instance.offlineVRRig, out var value))
		{
			int num = -1;
			num = (isLeft ? 1 : 3);
			grabber = value.rigActors[num].location.GetComponentInChildren<CrittersGrabber>();
			if (grabber != null)
			{
				grabber.isLeft = isLeft;
			}
		}
		if (grabber != null)
		{
			for (int i = 0; i < grabber.grabbedActors.Count; i++)
			{
				if (grabber.grabbedActors[i].localCanStore)
				{
					grabber.grabbedActors[i].CheckStorable();
				}
			}
		}
		if (transformToFollow != null)
		{
			base.transform.position = transformToFollow.position;
			base.transform.rotation = transformToFollow.rotation;
		}
		if (grabber == null)
		{
			return;
		}
		VerifyExistingGrab();
		validGrabTarget = FindGrabTargets();
		bool flag = ((!isLeft) ? ControllerInputPoller.instance.rightGrab : ControllerInputPoller.instance.leftGrab);
		bool num2 = (isLeft ? (EquipmentInteractor.instance.leftHandHeldEquipment != null) : (EquipmentInteractor.instance.rightHandHeldEquipment != null));
		if (num2)
		{
			flag = false;
		}
		if (!num2)
		{
			if (validGrabTarget.IsNotNull())
			{
				if (validGrabTarget != lastHover)
				{
					lastHover = validGrabTarget;
					DoHover();
				}
			}
			else
			{
				lastHover = null;
			}
		}
		if (!isGrabbing && flag)
		{
			isGrabbing = true;
			remainingGrabDuration = grabDuration;
		}
		else if (isGrabbing)
		{
			if (!flag)
			{
				isGrabbing = false;
				DoRelease();
			}
			else if (queuedGrab != null)
			{
				CheckApplyQueuedGrab();
			}
		}
		if (isGrabbing && remainingGrabDuration > 0f)
		{
			remainingGrabDuration -= Time.deltaTime;
			DoGrab();
		}
	}

	private CrittersActor FindGrabTargets()
	{
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, grabRadius, colliders, (int)CrittersManager.instance.objectLayers | (int)CrittersManager.instance.containerLayer);
		float num2 = 10000f;
		Collider collider = null;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				Rigidbody attachedRigidbody = colliders[i].attachedRigidbody;
				if (attachedRigidbody == null)
				{
					continue;
				}
				CrittersActor component = attachedRigidbody.GetComponent<CrittersActor>();
				if (!(component == null) && (!(component is CrittersBag) || !CrittersManager.instance.actorById.TryGetValue(component.parentActorId, out var value) || !(value is CrittersAttachPoint) || (value as CrittersAttachPoint).rigPlayerId != PhotonNetwork.LocalPlayer.ActorNumber || (value as CrittersAttachPoint).anchorLocation != CrittersAttachPoint.AnchoredLocationTypes.Arm || (value as CrittersAttachPoint).isLeft != isLeft) && component.usesRB && component.CanBeGrabbed(grabber))
				{
					float sqrMagnitude = (colliders[i].attachedRigidbody.position - base.transform.position).sqrMagnitude;
					if (sqrMagnitude < num2)
					{
						num2 = sqrMagnitude;
						collider = colliders[i];
					}
				}
			}
			if (collider == null)
			{
				return null;
			}
			return collider.attachedRigidbody.GetComponent<CrittersActor>();
		}
		return null;
	}

	private void DoHover()
	{
		validGrabTarget.OnHover(isLeft);
	}

	private void DoGrab()
	{
		if (!validGrabTarget.IsNull())
		{
			grabber.grabbing = true;
			if (isLeft)
			{
				EquipmentInteractor.instance.disableLeftGrab = true;
			}
			else
			{
				EquipmentInteractor.instance.disableRightGrab = true;
			}
			isHandGrabbingDisabled = true;
			remainingGrabDuration = 0f;
			Vector3 localOffset = grabber.transform.InverseTransformPoint(validGrabTarget.transform.position);
			Quaternion localRotation = grabber.transform.InverseTransformRotation(validGrabTarget.transform.rotation);
			if (validGrabTarget.IsCurrentlyAttachedToBag())
			{
				queuedGrab = validGrabTarget;
				queuedRelativeGrabOffset = localOffset;
				queuedRelativeGrabRotation = localRotation;
			}
			else if (validGrabTarget.AllowGrabbingActor(grabber))
			{
				ApplyGrab(validGrabTarget, localRotation, localOffset);
			}
		}
	}

	private void ApplyGrab(CrittersActor grabTarget, Quaternion localRotation, Vector3 localOffset)
	{
		if (grabTarget.AttemptSetEquipmentStorable())
		{
			RemoveGrabberPhysicsTrigger();
			AddGrabberPhysicsTrigger(grabTarget);
		}
		grabTarget.GrabbedBy(grabber, positionOverride: true, localRotation, localOffset);
		grabber.grabbedActors.Add(grabTarget);
		localGrabOffset = localOffset;
		CrittersPawn crittersPawn = grabTarget as CrittersPawn;
		if (crittersPawn.IsNotNull())
		{
			PlayHaptics(crittersPawn.grabbedHaptics, crittersPawn.grabbedHapticsStrength);
		}
	}

	private void DoRelease()
	{
		queuedGrab = null;
		grabber.grabbing = false;
		StopHaptics();
		for (int num = grabber.grabbedActors.Count - 1; num >= 0; num--)
		{
			CrittersActor crittersActor = grabber.grabbedActors[num];
			float magnitude = estimator.linearVelocity.magnitude;
			float num2 = magnitude + Mathf.Max(0f, magnitude - CrittersManager.instance.fastThrowThreshold) * CrittersManager.instance.fastThrowMultiplier;
			crittersActor.Released(keepWorldPosition: true, crittersActor.transform.rotation, crittersActor.transform.position, estimator.linearVelocity.normalized * num2, estimator.angularVelocity);
			if (num < grabber.grabbedActors.Count)
			{
				grabber.grabbedActors.RemoveAt(num);
			}
		}
		RemoveGrabberPhysicsTrigger();
		if (isHandGrabbingDisabled)
		{
			isHandGrabbingDisabled = false;
			if (isLeft)
			{
				EquipmentInteractor.instance.disableLeftGrab = false;
			}
			else
			{
				EquipmentInteractor.instance.disableRightGrab = false;
			}
		}
	}

	private void CheckApplyQueuedGrab()
	{
		if (Vector3.Magnitude(grabber.transform.InverseTransformPoint(queuedGrab.transform.position) - queuedRelativeGrabOffset) > grabDetachFromBagDist)
		{
			GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength / 4f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			if (queuedGrab.AllowGrabbingActor(grabber))
			{
				ApplyGrab(queuedGrab, queuedRelativeGrabRotation, queuedRelativeGrabOffset);
			}
			queuedGrab = null;
		}
	}

	private void VerifyExistingGrab()
	{
		for (int num = grabber.grabbedActors.Count - 1; num >= 0; num--)
		{
			CrittersActor crittersActor = grabber.grabbedActors[num];
			if (crittersActor.IsNull() || crittersActor.parentActorId != grabber.actorId)
			{
				if (grabber.IsNotNull())
				{
					grabber.grabbedActors.Remove(crittersActor);
				}
				RemoveGrabberPhysicsTrigger();
				StopHaptics();
			}
		}
	}

	public void PlayHaptics(AudioClip clip, float strength)
	{
		if (!(clip == null))
		{
			StopHaptics();
			playingHaptics = true;
			hapticsClip = clip;
			hapticsStrength = strength;
			hapticsLength = clip.length;
			haptics = StartCoroutine(PlayHapticsOnLoop());
		}
	}

	public void StopHaptics()
	{
		if (playingHaptics)
		{
			playingHaptics = false;
			StopCoroutine(haptics);
			haptics = null;
			GorillaTagger.Instance.StopHapticClip(isLeft);
		}
	}

	private IEnumerator PlayHapticsOnLoop()
	{
		while (true)
		{
			GorillaTagger.Instance.PlayHapticClip(isLeft, hapticsClip, hapticsStrength);
			yield return new WaitForSeconds(hapticsLength);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(other.attachedRigidbody == null))
		{
			CrittersActor component = other.attachedRigidbody.GetComponent<CrittersActor>();
			if (DoesActorActivateJoint(component, out var heldStorableActor))
			{
				ActivateJoints(component, heldStorableActor);
			}
		}
	}

	private void ActivateJoints(CrittersActor rigidJoint, CrittersActor softJoint)
	{
		softJoint.SetJointSoft(grabber.rb);
		if (rigidJoint.parentActorId != -1)
		{
			rigidJoint.SetJointRigid(CrittersManager.instance.actorById[rigidJoint.parentActorId].rb);
		}
		CrittersGrabberSharedData.AddEnteredActor(rigidJoint);
	}

	private bool DoesActorActivateJoint(CrittersActor potentialBagActor, out CrittersActor heldStorableActor)
	{
		heldStorableActor = null;
		for (int i = 0; i < grabber.grabbedActors.Count; i++)
		{
			if (grabber.grabbedActors[i].localCanStore)
			{
				heldStorableActor = grabber.grabbedActors[i];
			}
		}
		if (heldStorableActor == null)
		{
			return false;
		}
		if (!(potentialBagActor is CrittersBag))
		{
			return false;
		}
		if (CrittersManager.instance.actorById.TryGetValue(potentialBagActor.parentActorId, out var value) && value is CrittersAttachPoint && (value as CrittersAttachPoint).rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber && (value as CrittersAttachPoint).anchorLocation == CrittersAttachPoint.AnchoredLocationTypes.Arm && (value as CrittersAttachPoint).isLeft == isLeft)
		{
			return false;
		}
		return true;
	}

	private void AddGrabberPhysicsTrigger(CrittersActor actor)
	{
		CapsuleCollider capsuleCollider = CrittersManager.DuplicateCapsuleCollider(base.transform, actor.equipmentStoreTriggerCollider);
		capsuleCollider.isTrigger = true;
		triggerCollider = capsuleCollider;
		CrittersGrabberSharedData.AddTrigger(triggerCollider);
		rb.includeLayers = CrittersManager.instance.containerLayer;
	}

	private void RemoveGrabberPhysicsTrigger()
	{
		if (triggerCollider != null)
		{
			CrittersGrabberSharedData.RemoveTrigger(triggerCollider);
			UnityEngine.Object.Destroy(triggerCollider.gameObject);
		}
		triggerCollider = null;
		rb.includeLayers = 0;
	}

	private void NewJointMethod()
	{
		if (CrittersGrabberSharedData.triggerCollidersToCheck.Count == 0 && CrittersGrabberSharedData.enteredCritterActor.Count == 0)
		{
			return;
		}
		for (int i = 0; i < CrittersGrabberSharedData.actorGrabbers.Count; i++)
		{
			CrittersGrabberSharedData.actorGrabbers[i].actorsStillPresent.Clear();
			CapsuleCollider capsuleCollider = CrittersGrabberSharedData.actorGrabbers[i].triggerCollider;
			if (capsuleCollider == null)
			{
				continue;
			}
			Vector3 vector = capsuleCollider.transform.up * MathF.Max(0f, capsuleCollider.height / 2f - capsuleCollider.radius);
			int num = Physics.OverlapCapsuleNonAlloc(capsuleCollider.transform.position + vector, capsuleCollider.transform.position - vector, capsuleCollider.radius, colliders, CrittersManager.instance.containerLayer, QueryTriggerInteraction.Collide);
			if (num == 0)
			{
				continue;
			}
			for (int j = 0; j < num; j++)
			{
				Rigidbody attachedRigidbody = colliders[j].attachedRigidbody;
				if (!(attachedRigidbody == null))
				{
					CrittersActor component = attachedRigidbody.GetComponent<CrittersActor>();
					if (!(component == null) && !CrittersGrabberSharedData.actorGrabbers[i].actorsStillPresent.Contains(component))
					{
						CrittersGrabberSharedData.actorGrabbers[i].actorsStillPresent.Add(component);
					}
				}
			}
		}
		for (int k = 0; k < CrittersGrabberSharedData.actorGrabbers.Count; k++)
		{
			CrittersActorGrabber crittersActorGrabber = CrittersGrabberSharedData.actorGrabbers[k];
			for (int l = 0; l < CrittersGrabberSharedData.actorGrabbers[k].actorsStillPresent.Count; l++)
			{
				CrittersActor crittersActor = CrittersGrabberSharedData.actorGrabbers[k].actorsStillPresent[l];
				if (crittersActorGrabber.DoesActorActivateJoint(crittersActor, out var heldStorableActor))
				{
					crittersActorGrabber.ActivateJoints(crittersActor, heldStorableActor);
				}
			}
		}
		for (int num2 = CrittersGrabberSharedData.enteredCritterActor.Count - 1; num2 >= 0; num2--)
		{
			CrittersActor crittersActor2 = CrittersGrabberSharedData.enteredCritterActor[num2];
			bool flag = false;
			for (int m = 0; m < CrittersGrabberSharedData.actorGrabbers.Count; m++)
			{
				flag |= CrittersGrabberSharedData.actorGrabbers[m].actorsStillPresent.Contains(crittersActor2);
			}
			if (!flag)
			{
				CrittersGrabberSharedData.RemoveEnteredActor(crittersActor2);
				crittersActor2.DisconnectJoint();
			}
		}
		CrittersGrabberSharedData.DisableEmptyGrabberJoints();
	}
}
