using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameText : MonoBehaviour, IBuildValidation
{
	private enum Mode
	{
		CosmeticOwner,
		LocalPlayer,
		NearestPlayer,
		NearestOtherPlayer,
		FurthestPlayer,
		FurthestOtherPlayer,
		RandomPlayer,
		RandomOtherPlayer
	}

	[SerializeField]
	private Mode mode;

	[SerializeField]
	private TMP_Text tmp_text;

	private void OnEnable()
	{
		FetchName();
	}

	public void FetchName()
	{
		VRRig vRRig = null;
		List<VRRig> list = new List<VRRig>();
		switch (mode)
		{
		case Mode.CosmeticOwner:
			vRRig = GetComponentInParent<VRRig>();
			break;
		case Mode.LocalPlayer:
			vRRig = VRRig.LocalRig;
			break;
		case Mode.RandomPlayer:
			VRRigCache.Instance.GetActiveRigs(list);
			if (list.Count > 0)
			{
				vRRig = list[Random.Range(0, list.Count)];
			}
			break;
		case Mode.RandomOtherPlayer:
			VRRigCache.Instance.GetAllUsedRigs(list);
			if (list.Count > 0)
			{
				vRRig = list[Random.Range(0, list.Count)];
			}
			break;
		case Mode.NearestPlayer:
			VRRigCache.Instance.GetActiveRigs(list);
			vRRig = getNearest(list);
			break;
		case Mode.NearestOtherPlayer:
			VRRigCache.Instance.GetAllUsedRigs(list);
			vRRig = getNearest(list);
			break;
		case Mode.FurthestPlayer:
			VRRigCache.Instance.GetActiveRigs(list);
			vRRig = getFurthest(list);
			break;
		case Mode.FurthestOtherPlayer:
			VRRigCache.Instance.GetAllUsedRigs(list);
			vRRig = getFurthest(list);
			break;
		}
		tmp_text.text = ((vRRig != null) ? vRRig.playerNameVisible : "MONKE");
	}

	private VRRig getNearest(List<VRRig> rigs)
	{
		VRRig result = null;
		float num = float.PositiveInfinity;
		for (int i = 0; i < rigs.Count; i++)
		{
			float num2 = Vector3.Distance(rigs[i].transform.position, base.transform.position);
			if (num2 < num)
			{
				result = rigs[i];
				num = num2;
			}
		}
		return result;
	}

	private VRRig getFurthest(List<VRRig> rigs)
	{
		VRRig result = null;
		float num = 0f;
		for (int i = 0; i < rigs.Count; i++)
		{
			float num2 = Vector3.Distance(rigs[i].transform.position, base.transform.position);
			if (num2 > num)
			{
				result = rigs[i];
				num = num2;
			}
		}
		return result;
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (tmp_text == null)
		{
			Debug.Log("Hey Monke!! You gotta gimme a TMP_Text object!");
			return false;
		}
		return true;
	}
}
