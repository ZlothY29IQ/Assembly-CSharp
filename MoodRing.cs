using GorillaTag;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;

public class MoodRing : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private bool attachedToLeftHand;

	private VRRig myRig;

	[SerializeField]
	private float rotationSpeed;

	[SerializeField]
	private float furCycleSpeed;

	private float nextFurCycleTimestamp;

	private float animRedValue;

	private float animGreenValue;

	private float animBlueValue;

	private bool isCycling;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnDespawn()
	{
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	private void Update()
	{
		if ((attachedToLeftHand ? myRig.leftIndex.calcT : myRig.rightIndex.calcT) > 0.5f)
		{
			if (!isCycling)
			{
				animRedValue = myRig.playerColor.r;
				animGreenValue = myRig.playerColor.g;
				animBlueValue = myRig.playerColor.b;
			}
			isCycling = true;
			RainbowCycle(ref animRedValue, ref animGreenValue, ref animBlueValue);
			myRig.InitializeNoobMaterialLocal(animRedValue, animGreenValue, animBlueValue);
		}
		else
		{
			if (!isCycling)
			{
				return;
			}
			isCycling = false;
			if (myRig.isOfflineVRRig)
			{
				animRedValue = Mathf.Round(animRedValue * 9f) / 9f;
				animGreenValue = Mathf.Round(animGreenValue * 9f) / 9f;
				animBlueValue = Mathf.Round(animBlueValue * 9f) / 9f;
				GorillaTagger.Instance.UpdateColor(animRedValue, animGreenValue, animBlueValue);
				if (NetworkSystem.Instance.InRoom)
				{
					GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, animRedValue, animGreenValue, animBlueValue);
				}
				PlayerPrefs.SetFloat("redValue", animRedValue);
				PlayerPrefs.SetFloat("greenValue", animGreenValue);
				PlayerPrefs.SetFloat("blueValue", animBlueValue);
				PlayerPrefs.Save();
			}
		}
	}

	private void RainbowCycle(ref float r, ref float g, ref float b)
	{
		float num = furCycleSpeed * Time.deltaTime;
		if (r == 1f)
		{
			if (b > 0f)
			{
				b = Mathf.Clamp01(b - num);
			}
			else if (g < 1f)
			{
				g = Mathf.Clamp01(g + num);
			}
			else
			{
				r = Mathf.Clamp01(r - num);
			}
		}
		else if (g == 1f)
		{
			if (r > 0f)
			{
				r = Mathf.Clamp01(r - num);
			}
			else if (b < 1f)
			{
				b = Mathf.Clamp01(b + num);
			}
			else
			{
				g = Mathf.Clamp01(g - num);
			}
		}
		else if (b == 1f)
		{
			if (g > 0f)
			{
				g = Mathf.Clamp01(g - num);
			}
			else if (r < 1f)
			{
				r = Mathf.Clamp01(r + num);
			}
			else
			{
				b = Mathf.Clamp01(b - num);
			}
		}
		else
		{
			r = Mathf.Clamp01(r + num);
		}
	}
}
