using System;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderSmallMonkeTrigger : MonoBehaviour
{
	private int lastTriggeredFrame = -1;

	private List<Collider> overlappingColliders = new List<Collider>(20);

	private bool hasCheckedZone;

	private bool ignoreScale;

	public int overlapCount => overlappingColliders.Count;

	public bool TriggeredThisFrame => lastTriggeredFrame == Time.frameCount;

	public event Action<int> onPlayerEnteredTrigger;

	public event Action onTriggerFirstEntered;

	public event Action onTriggerLastExited;

	public void ValidateOverlappingColliders()
	{
		for (int num = overlappingColliders.Count - 1; num >= 0; num--)
		{
			if (overlappingColliders[num] == null || !overlappingColliders[num].gameObject.activeInHierarchy || !overlappingColliders[num].enabled)
			{
				overlappingColliders.RemoveAt(num);
			}
			else
			{
				VRRig vRRig = overlappingColliders[num].attachedRigidbody.gameObject.GetComponent<VRRig>();
				if (vRRig == null)
				{
					if (GTPlayer.Instance.bodyCollider == overlappingColliders[num] || GTPlayer.Instance.headCollider == overlappingColliders[num])
					{
						vRRig = GorillaTagger.Instance.offlineVRRig;
					}
					else
					{
						overlappingColliders.RemoveAt(num);
					}
				}
				if (!ignoreScale && vRRig != null && (double)vRRig.scaleFactor > 0.99)
				{
					overlappingColliders.RemoveAt(num);
				}
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody == null)
		{
			return;
		}
		VRRig vRRig = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (vRRig == null)
		{
			if (!(GTPlayer.Instance.bodyCollider == other) && !(GTPlayer.Instance.headCollider == other))
			{
				return;
			}
			vRRig = GorillaTagger.Instance.offlineVRRig;
		}
		if (!hasCheckedZone)
		{
			if (BuilderTable.TryGetBuilderTableForZone(vRRig.zoneEntity.currentZone, out var table))
			{
				ignoreScale = !table.isTableMutable;
			}
			hasCheckedZone = true;
		}
		if (ignoreScale || !((double)vRRig.scaleFactor > 0.99))
		{
			if (vRRig != null)
			{
				this.onPlayerEnteredTrigger?.Invoke(vRRig.OwningNetPlayer.ActorNumber);
			}
			bool num = overlappingColliders.Count == 0;
			if (!overlappingColliders.Contains(other))
			{
				overlappingColliders.Add(other);
			}
			lastTriggeredFrame = Time.frameCount;
			if (num)
			{
				this.onTriggerFirstEntered?.Invoke();
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (overlappingColliders.Remove(other) && overlappingColliders.Count == 0)
		{
			this.onTriggerLastExited?.Invoke();
		}
	}
}
