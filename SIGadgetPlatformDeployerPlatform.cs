using System;
using UnityEngine;

public class SIGadgetPlatformDeployerPlatform : MonoBehaviour, ISIGameDeployable
{
	[SerializeField]
	private GameObject extendedDurationFrame;

	[SerializeField]
	private float defaultDuration = 10f;

	[SerializeField]
	private float extendedDuration = 20f;

	[SerializeField]
	private BoxCollider activeCollider;

	private bool isOverlappingHead;

	private float timeToDie = -1f;

	private Bounds checkBounds;

	private Vector3 checkOffset;

	private Quaternion checkRot;

	public Action OnDisabled;

	public void ApplyUpgrades(SIUpgradeSet upgrades)
	{
		bool flag = upgrades.Contains(SIUpgradeType.Platform_Duration);
		float num = (flag ? extendedDuration : defaultDuration);
		timeToDie = Time.time + num;
		extendedDurationFrame.SetActive(flag);
		checkBounds = new Bounds(activeCollider.center, activeCollider.size);
		Vector3 size = checkBounds.size;
		Vector3 lossyScale = activeCollider.transform.lossyScale;
		size.x *= lossyScale.x;
		size.y *= lossyScale.y;
		size.z *= lossyScale.z;
		checkBounds.size = size;
		checkOffset = activeCollider.transform.position;
		checkRot = activeCollider.transform.rotation;
		CheckHeadOverlap();
	}

	public void CheckHeadOverlap()
	{
		if (!(activeCollider == null))
		{
			Vector3 position = GorillaTagger.Instance.headCollider.transform.position;
			float num = GorillaTagger.Instance.headCollider.radius * GorillaTagger.Instance.headCollider.transform.lossyScale.x;
			Vector3 vector = Quaternion.Inverse(checkRot) * (position - checkOffset);
			if (Vector3.Magnitude(checkBounds.ClosestPoint(vector) - vector) < num)
			{
				isOverlappingHead = true;
				activeCollider.enabled = false;
			}
			else
			{
				isOverlappingHead = false;
				activeCollider.enabled = true;
			}
		}
	}

	private void LateUpdate()
	{
		if (Time.time > timeToDie)
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
		else if (isOverlappingHead)
		{
			CheckHeadOverlap();
		}
	}

	private void OnDisable()
	{
		OnDisabled?.Invoke();
		OnDisabled = null;
	}
}
