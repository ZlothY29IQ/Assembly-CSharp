using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaLocomotion.Gameplay;

public class OldGorillaRopeSwing : MonoBehaviourPun
{
	public const float kPlayerMass = 0.8f;

	public const float ropeBitGenOffset = 1f;

	public const float MAX_ROPE_SPEED = 15f;

	[SerializeField]
	private GameObject prefabRopeBit;

	public Rigidbody[] bones = Array.Empty<Rigidbody>();

	private Dictionary<int, int> remotePlayers = new Dictionary<int, int>();

	[NonSerialized]
	public float lastGrabTime;

	[SerializeField]
	private AudioSource ropeCreakSFX;

	private bool localPlayerOn;

	private XRNode localPlayerXRNode;

	private Rigidbody localGrabbedRigid;

	private const float MAX_VELOCITY_FOR_IDLE = 0.1f;

	private const float TIME_FOR_IDLE = 2f;

	private float potentialIdleTimer;

	[Header("Config")]
	[SerializeField]
	private int ropeLength = 8;

	[SerializeField]
	private GorillaRopeSwingSettings settings;

	public bool isIdle { get; private set; }

	private void Awake()
	{
		SetIsIdle(idle: true);
	}

	private void OnDisable()
	{
		if (!isIdle)
		{
			SetIsIdle(idle: true);
		}
	}

	private void Update()
	{
		if (localPlayerOn && (bool)localGrabbedRigid)
		{
			float magnitude = localGrabbedRigid.linearVelocity.magnitude;
			if (magnitude > 2.5f && !ropeCreakSFX.isPlaying && Mathf.RoundToInt(Time.time) % 5 == 0)
			{
				ropeCreakSFX.GTPlay();
			}
			float num = MathUtils.Linear(magnitude, 0f, 10f, -0.07f, 0.5f);
			if (num > 0f)
			{
				GorillaTagger.Instance.DoVibration(localPlayerXRNode, num, Time.deltaTime);
			}
		}
		if (isIdle)
		{
			return;
		}
		if (!localPlayerOn && remotePlayers.Count == 0)
		{
			Rigidbody[] array = bones;
			foreach (Rigidbody rigidbody in array)
			{
				float magnitude2 = rigidbody.linearVelocity.magnitude;
				float num2 = Time.deltaTime * settings.frictionWhenNotHeld;
				if (num2 >= magnitude2 - 0.1f)
				{
					num2 = 0f;
				}
				else
				{
					rigidbody.linearVelocity = Vector3.MoveTowards(rigidbody.linearVelocity, Vector3.zero, num2);
				}
			}
		}
		bool flag = false;
		for (int j = 0; j < bones.Length; j++)
		{
			if (bones[j].linearVelocity.magnitude > 0.1f)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			potentialIdleTimer += Time.deltaTime;
		}
		else
		{
			potentialIdleTimer = 0f;
		}
		if (potentialIdleTimer >= 2f)
		{
			SetIsIdle(idle: true);
			potentialIdleTimer = 0f;
		}
	}

	private void SetIsIdle(bool idle)
	{
		isIdle = idle;
		ToggleIsKinematic(idle);
		if (idle)
		{
			for (int i = 0; i < bones.Length; i++)
			{
				bones[i].linearVelocity = Vector3.zero;
				bones[i].angularVelocity = Vector3.zero;
				bones[i].transform.localRotation = Quaternion.identity;
			}
		}
	}

	private void ToggleIsKinematic(bool kinematic)
	{
		for (int i = 0; i < bones.Length; i++)
		{
			bones[i].isKinematic = kinematic;
			if (kinematic)
			{
				bones[i].interpolation = RigidbodyInterpolation.None;
			}
			else
			{
				bones[i].interpolation = RigidbodyInterpolation.Interpolate;
			}
		}
	}

	public Rigidbody GetBone(int index)
	{
		if (index >= bones.Length)
		{
			return bones.Last();
		}
		return bones[index];
	}

	public int GetBoneIndex(Rigidbody r)
	{
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] == r)
			{
				return i;
			}
		}
		return bones.Length - 1;
	}

	public void AttachLocalPlayer(XRNode xrNode, Rigidbody rigid, Vector3 offset, Vector3 velocity)
	{
		int boneIndex = GetBoneIndex(rigid);
		velocity *= settings.inheritVelocityMultiplier;
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = base.photonView.ViewID;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeBoneIndex = boneIndex;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsLeft = xrNode == XRNode.LeftHand;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeOffset = offset;
		}
		List<Vector3> list = new List<Vector3>();
		List<Vector3> list2 = new List<Vector3>();
		if (remotePlayers.Count <= 0)
		{
			Rigidbody[] array = bones;
			foreach (Rigidbody rigidbody in array)
			{
				list.Add(rigidbody.transform.localEulerAngles);
				list2.Add(rigidbody.linearVelocity);
			}
		}
		if (Time.time - lastGrabTime > 1f && (remotePlayers.Count == 0 || velocity.magnitude > 2f))
		{
			SetVelocity_RPC(boneIndex, velocity, wholeRope: true, list.ToArray(), list2.ToArray());
		}
		lastGrabTime = Time.time;
		ropeCreakSFX.transform.parent = GetBone(Math.Max(0, boneIndex - 2)).transform;
		ropeCreakSFX.transform.localPosition = Vector3.zero;
		localPlayerOn = true;
		localPlayerXRNode = xrNode;
		localGrabbedRigid = rigid;
	}

	public void DetachLocalPlayer()
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = -1;
		}
		localPlayerOn = false;
		localGrabbedRigid = null;
	}

	public bool AttachRemotePlayer(int playerId, int boneIndex, Transform offsetTransform, Vector3 offset)
	{
		Rigidbody bone = GetBone(boneIndex);
		if (bone == null)
		{
			return false;
		}
		offsetTransform.SetParent(bone.transform);
		offsetTransform.localPosition = offset;
		offsetTransform.localRotation = Quaternion.identity;
		if (remotePlayers.ContainsKey(playerId))
		{
			Debug.LogError("already on the list!");
			return false;
		}
		remotePlayers.Add(playerId, boneIndex);
		return true;
	}

	public void DetachRemotePlayer(int playerId)
	{
		remotePlayers.Remove(playerId);
	}

	public void SetVelocity_RPC(int boneIndex, Vector3 velocity, bool wholeRope = true, Vector3[] ropeRotations = null, Vector3[] ropeVelocities = null)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			base.photonView.RPC("SetVelocity", RpcTarget.All, boneIndex, velocity, wholeRope, ropeRotations, ropeVelocities);
		}
		else
		{
			SetVelocity(boneIndex, velocity, wholeRope, ropeRotations, ropeVelocities);
		}
	}

	[PunRPC]
	public void SetVelocity(int boneIndex, Vector3 velocity, bool wholeRope = true, Vector3[] ropeRotations = null, Vector3[] ropeVelocities = null)
	{
		SetIsIdle(idle: false);
		if (ropeRotations != null && ropeVelocities != null && ropeRotations.Length != 0)
		{
			ToggleIsKinematic(kinematic: true);
			for (int i = 0; i < ropeRotations.Length; i++)
			{
				if (i != 0)
				{
					bones[i].transform.localRotation = Quaternion.Euler(ropeRotations[i]);
					bones[i].linearVelocity = ropeVelocities[i];
				}
			}
			ToggleIsKinematic(kinematic: false);
		}
		Rigidbody bone = GetBone(boneIndex);
		if (!bone)
		{
			return;
		}
		if (wholeRope)
		{
			int num = 0;
			float maxLength = Mathf.Min(velocity.magnitude, 15f);
			Rigidbody[] array = bones;
			foreach (Rigidbody obj in array)
			{
				Vector3 vector = velocity / boneIndex * num;
				vector = Vector3.ClampMagnitude(vector, maxLength);
				obj.linearVelocity = vector;
				num++;
			}
		}
		else
		{
			bone.linearVelocity = velocity;
		}
	}
}
