using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using GorillaLocomotion.Swimming;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CustomMapsGorillaRopeSwing : GorillaRopeSwing
{
	[SerializeField]
	private GameObject partiallyUnderwaterPrefab;

	private bool isRopeLengthSet;

	private List<RopeSwingSegment> preExistingSegments;

	private GTObjectPlaceholder ropePlaceholder;

	private Vector3 ropeScale = Vector3.one;

	public bool snapX;

	public bool snapY;

	public bool snapZ;

	public float maxDistanceSnap = 0.05f;

	public AudioClip onGrabSFX;

	public AudioClip OnReleaseSFX;

	protected override void Awake()
	{
		CalculateId(force: true);
		StartCoroutine(WaitForRopeLength());
	}

	protected override void Start()
	{
	}

	protected override void OnEnable()
	{
		if (isRopeLengthSet)
		{
			base.OnEnable();
		}
	}

	public void SetRopeLength(int length)
	{
		ropeLength = length;
		isRopeLengthSet = true;
	}

	public void SetRopeProperties(GTObjectPlaceholder placeholder)
	{
		ropePlaceholder = placeholder;
		ropeLength = ropePlaceholder.ropeLength;
		ropeBitGenOffset = ropePlaceholder.ropeSegmentGenerationOffset;
		preExistingSegments = ropePlaceholder.ropeSwingSegments;
		ropeScale = ropePlaceholder.transform.localScale;
		base.transform.localScale = Vector3.one;
		isRopeLengthSet = true;
	}

	private IEnumerator WaitForRopeLength()
	{
		while (!isRopeLengthSet)
		{
			yield return null;
		}
		RopeGeneration();
		base.Awake();
		base.OnEnable();
		base.Start();
	}

	private void RopeGeneration()
	{
		List<Transform> list = new List<Transform>();
		if (preExistingSegments != null && preExistingSegments.Count > 0)
		{
			for (int i = 0; i < preExistingSegments.Count; i++)
			{
				preExistingSegments[i].transform.SetParent(base.transform);
				GorillaClimbable gorillaClimbable = preExistingSegments[i].AddComponent<GorillaClimbable>();
				gorillaClimbable.snapX = snapX;
				gorillaClimbable.snapY = snapY;
				gorillaClimbable.snapZ = snapZ;
				gorillaClimbable.maxDistanceSnap = maxDistanceSnap;
				gorillaClimbable.clip = onGrabSFX;
				gorillaClimbable.clipOnFullRelease = OnReleaseSFX;
				GorillaRopeSegment gorillaRopeSegment = preExistingSegments[i].AddComponent<GorillaRopeSegment>();
				gorillaRopeSegment.swing = this;
				gorillaRopeSegment.boneIndex = preExistingSegments[i].boneIndex;
				list.Add(preExistingSegments[i].transform);
			}
			base.transform.localScale = ropeScale;
			ropePlaceholder.transform.localScale = Vector3.one;
		}
		else
		{
			Vector3 zero = Vector3.zero;
			float y = prefabRopeBit.GetComponentInChildren<Renderer>().bounds.size.y;
			WaterVolume[] array = Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None);
			List<Collider> list2 = new List<Collider>(array.Length);
			WaterVolume[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				foreach (Collider volumeCollider in array2[j].volumeColliders)
				{
					if (!(volumeCollider == null))
					{
						list2.Add(volumeCollider);
					}
				}
			}
			for (int k = 0; k < ropeLength + 1; k++)
			{
				bool flag = false;
				if (list2.Count > 0)
				{
					Collider collider = list2[0];
					if (collider != null)
					{
						Vector3 vector = base.transform.position + zero;
						Vector3 point = vector + new Vector3(0f, 0f - y, 0f);
						flag = collider.bounds.Contains(vector) || collider.bounds.Contains(point);
					}
				}
				GameObject gameObject = Object.Instantiate(flag ? partiallyUnderwaterPrefab : prefabRopeBit, base.transform);
				gameObject.name = $"RopeBone_{k:00}";
				gameObject.transform.localPosition = zero;
				gameObject.transform.localRotation = Quaternion.identity;
				zero += new Vector3(0f, 0f - ropeBitGenOffset, 0f);
				GorillaRopeSegment component = gameObject.GetComponent<GorillaRopeSegment>();
				component.swing = this;
				component.boneIndex = k;
				list.Add(gameObject.transform);
			}
			list[0].GetComponent<BoxCollider>().center = new Vector3(0f, -0.65f, 0f);
			list[0].GetComponent<BoxCollider>().size = new Vector3(0.3f, 0.65f, 0.3f);
		}
		if (list.Count > 0)
		{
			list.Last().gameObject.SetActive(value: false);
		}
		nodes = list.ToArray();
	}
}
