using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;

public class DrumsItem : MonoBehaviour, ISpawnable
{
	[Tooltip("Array of colliders for this specific drum.")]
	public Collider[] collidersForThisDrum;

	private List<Collider> collidersForThisDrumList = new List<Collider>();

	[Tooltip("AudioSources where each index must match the index given to the corresponding Drum component.")]
	public AudioSource[] drumsAS;

	[Tooltip("Max volume a drum can reach.")]
	public float maxDrumVolume = 0.2f;

	[Tooltip("Min volume a drum can reach.")]
	public float minDrumVolume = 0.05f;

	[Tooltip("Multiplies against actual velocity before capping by min & maxDrumVolume values.")]
	public float maxDrumVolumeVelocity = 1f;

	private bool rightHandIn;

	private bool leftHandIn;

	private float volToPlay;

	private GorillaTriggerColliderHandIndicator rightHandIndicator;

	private GorillaTriggerColliderHandIndicator leftHandIndicator;

	private RaycastHit[] collidersHit = new RaycastHit[20];

	private Collider[] actualColliders = new Collider[20];

	public LayerMask drumsTouchable;

	private float sphereRadius;

	private Vector3 spherecastSweep;

	private int collidersHitCount;

	private List<RaycastHit> hitList = new List<RaycastHit>(20);

	private Drum tempDrum;

	private bool drumHit;

	private RaycastHit nullHit;

	public int onlineOffset;

	[Tooltip("VRRig object of the player, used to determine if it is an offline rig.")]
	private VRRig myRig;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
		leftHandIndicator = GorillaTagger.Instance.leftHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		rightHandIndicator = GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
		sphereRadius = leftHandIndicator.GetComponent<SphereCollider>().radius;
		for (int i = 0; i < collidersForThisDrum.Length; i++)
		{
			collidersForThisDrumList.Add(collidersForThisDrum[i]);
		}
		for (int j = 0; j < drumsAS.Length; j++)
		{
			myRig.AssignDrumToMusicDrums(j + onlineOffset, drumsAS[j]);
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	private void LateUpdate()
	{
		CheckHandHit(ref leftHandIn, ref leftHandIndicator, isLeftHand: true);
		CheckHandHit(ref rightHandIn, ref rightHandIndicator, isLeftHand: false);
	}

	private void CheckHandHit(ref bool handIn, ref GorillaTriggerColliderHandIndicator handIndicator, bool isLeftHand)
	{
		spherecastSweep = handIndicator.transform.position - handIndicator.lastPosition;
		if (spherecastSweep.magnitude < 0.0001f)
		{
			spherecastSweep = Vector3.up * 0.0001f;
		}
		for (int i = 0; i < collidersHit.Length; i++)
		{
			collidersHit[i] = nullHit;
		}
		collidersHitCount = Physics.SphereCastNonAlloc(handIndicator.lastPosition, sphereRadius, spherecastSweep.normalized, collidersHit, spherecastSweep.magnitude, drumsTouchable, QueryTriggerInteraction.Collide);
		drumHit = false;
		if (collidersHitCount > 0)
		{
			hitList.Clear();
			for (int j = 0; j < collidersHit.Length; j++)
			{
				if (collidersHit[j].collider != null && collidersForThisDrumList.Contains(collidersHit[j].collider) && collidersHit[j].collider.gameObject.activeSelf)
				{
					hitList.Add(collidersHit[j]);
				}
			}
			hitList.Sort(RayCastHitCompare);
			for (int k = 0; k < hitList.Count; k++)
			{
				tempDrum = hitList[k].collider.GetComponent<Drum>();
				if (tempDrum != null)
				{
					drumHit = true;
					if (!handIn && !tempDrum.disabler)
					{
						DrumHit(tempDrum, isLeftHand, handIndicator.currentVelocity.magnitude);
					}
					break;
				}
			}
		}
		if (!drumHit & handIn)
		{
			GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration);
		}
		handIn = drumHit;
	}

	private int RayCastHitCompare(RaycastHit a, RaycastHit b)
	{
		if (a.distance < b.distance)
		{
			return -1;
		}
		if (a.distance == b.distance)
		{
			return 0;
		}
		return 1;
	}

	public void DrumHit(Drum tempDrumInner, bool isLeftHand, float hitVelocity)
	{
		if (isLeftHand)
		{
			if (leftHandIn)
			{
				return;
			}
			leftHandIn = true;
		}
		else
		{
			if (rightHandIn)
			{
				return;
			}
			rightHandIn = true;
		}
		volToPlay = Mathf.Max(Mathf.Min(1f, hitVelocity / maxDrumVolumeVelocity) * maxDrumVolume, minDrumVolume);
		if (NetworkSystem.Instance.InRoom)
		{
			if (!myRig.isOfflineVRRig)
			{
				myRig.netView?.SendRPC("RPC_PlayDrum", RpcTarget.Others, tempDrumInner.myIndex + onlineOffset, volToPlay);
			}
			else
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayDrum", RpcTarget.Others, tempDrumInner.myIndex + onlineOffset, volToPlay);
			}
		}
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 4f, GorillaTagger.Instance.tapHapticDuration);
		drumsAS[tempDrumInner.myIndex].volume = maxDrumVolume;
		drumsAS[tempDrumInner.myIndex].GTPlayOneShot(drumsAS[tempDrumInner.myIndex].clip, volToPlay);
	}
}
