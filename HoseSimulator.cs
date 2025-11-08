using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class HoseSimulator : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private SkinnedMeshRenderer skinnedMeshRenderer;

	[SerializeField]
	private Vector3 localBoundsOverride;

	[SerializeField]
	private Transform[] miscBones;

	[SerializeField]
	private Transform[] hoseBones;

	[SerializeField]
	private float[] hoseBoneMaxDisplacement;

	[SerializeField]
	private CosmeticRefID startAnchorRef;

	private Transform startAnchor;

	[SerializeField]
	private float startStiffness = 0.5f;

	[SerializeField]
	private Transform endAnchor;

	[SerializeField]
	private float endStiffness = 0.5f;

	private Vector3[] hoseBonePositions;

	private Vector3[] hoseBoneVelocities;

	[SerializeField]
	private float damping = 0.97f;

	private float[] hoseSectionLengths;

	private float totalHoseLength;

	private bool firstUpdate = true;

	private HoseSimulatorAnchors anchors;

	[SerializeField]
	private TransferrableObject myHoldable;

	private bool isLeftHanded;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnDespawn()
	{
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		anchors = rig.cosmeticReferences.Get(startAnchorRef).GetComponent<HoseSimulatorAnchors>();
		if (skinnedMeshRenderer != null)
		{
			Bounds localBounds = skinnedMeshRenderer.localBounds;
			localBounds.extents = localBoundsOverride;
			skinnedMeshRenderer.localBounds = localBounds;
		}
		hoseSectionLengths = new float[hoseBones.Length - 1];
		hoseBonePositions = new Vector3[hoseBones.Length];
		hoseBoneVelocities = new Vector3[hoseBones.Length];
		for (int i = 0; i < hoseSectionLengths.Length; i++)
		{
			float num = 1f;
			hoseSectionLengths[i] = num;
			totalHoseLength += num;
		}
	}

	private void LateUpdate()
	{
		if (myHoldable.InLeftHand())
		{
			isLeftHanded = true;
		}
		else if (myHoldable.InRightHand())
		{
			isLeftHanded = false;
		}
		for (int i = 0; i < miscBones.Length; i++)
		{
			Transform transform = (isLeftHanded ? anchors.miscAnchorsLeft[i] : anchors.miscAnchorsRight[i]);
			miscBones[i].transform.position = transform.position;
			miscBones[i].transform.rotation = transform.rotation;
		}
		startAnchor = (isLeftHanded ? anchors.leftAnchorPoint : anchors.rightAnchorPoint);
		float x = myHoldable.transform.lossyScale.x;
		float num = 0f;
		Vector3 position = startAnchor.position;
		Vector3 ctrl = position + startAnchor.forward * startStiffness * x;
		Vector3 position2 = endAnchor.position;
		Vector3 ctrl2 = position2 - endAnchor.forward * endStiffness * x;
		for (int j = 0; j < hoseBones.Length; j++)
		{
			float num2 = num / totalHoseLength;
			Vector3 vector = BezierUtils.BezierSolve(num2, position, ctrl, ctrl2, position2);
			Vector3 vector2 = BezierUtils.BezierSolve(num2 + 0.1f, position, ctrl, ctrl2, position2);
			if (firstUpdate)
			{
				hoseBones[j].transform.position = vector;
				hoseBonePositions[j] = vector;
				hoseBoneVelocities[j] = Vector3.zero;
			}
			else
			{
				hoseBoneVelocities[j] *= damping;
				hoseBonePositions[j] += hoseBoneVelocities[j] * Time.deltaTime;
				float num3 = hoseBoneMaxDisplacement[j] * x;
				if ((vector - hoseBonePositions[j]).IsLongerThan(num3))
				{
					Vector3 vector3 = vector + (hoseBonePositions[j] - vector).normalized * num3;
					hoseBoneVelocities[j] += (vector3 - hoseBonePositions[j]) / Time.deltaTime;
					hoseBonePositions[j] = vector3;
				}
				hoseBones[j].transform.position = hoseBonePositions[j];
			}
			hoseBones[j].transform.rotation = Quaternion.LookRotation(vector2 - vector, endAnchor.transform.up);
			if (j < hoseSectionLengths.Length)
			{
				num += hoseSectionLengths[j];
			}
		}
		firstUpdate = false;
	}

	private void OnDrawGizmosSelected()
	{
		if (hoseBonePositions != null)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLineStrip(hoseBonePositions, looped: false);
		}
	}
}
