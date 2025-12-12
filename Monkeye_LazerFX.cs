using UnityEngine;

public class Monkeye_LazerFX : MonoBehaviour
{
	private Transform[] eyeBones;

	private VRRig targetRig;

	private Vector3 targetPos;

	public LineRenderer[] lines;

	public GameObject targetFx;

	private void Awake()
	{
		base.enabled = false;
		LineRenderer[] array = lines;
		foreach (LineRenderer obj in array)
		{
			obj.positionCount = 2;
			obj.enabled = false;
		}
		if (targetFx != null)
		{
			targetFx.SetActive(value: false);
		}
	}

	public void EnableLazer(Transform[] eyes_, VRRig rig_, float maxDist = 10000f)
	{
		if (!(rig_ == targetRig))
		{
			eyeBones = eyes_;
			targetRig = rig_;
			targetPos = targetRig.transform.position;
			base.enabled = true;
			LineRenderer[] array = lines;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			if (targetFx != null)
			{
				targetFx.transform.position = targetPos;
				targetFx.SetActive(value: true);
			}
		}
	}

	public void EnableLazer(Transform[] eyes_, Vector3 targetPos_)
	{
		eyeBones = eyes_;
		targetRig = null;
		targetPos = targetPos_;
		base.enabled = true;
		LineRenderer[] array = lines;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		if (targetFx != null)
		{
			targetFx.transform.position = targetPos;
			targetFx.SetActive(value: true);
		}
	}

	public void DisableLazer()
	{
		targetRig = null;
		if (base.enabled)
		{
			base.enabled = false;
			LineRenderer[] array = lines;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			if (targetFx != null)
			{
				targetFx.SetActive(value: false);
			}
		}
	}

	private void Update()
	{
		if (targetRig != null)
		{
			targetPos = targetRig.transform.position;
		}
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i].SetPosition(0, eyeBones[i].transform.position);
			lines[i].SetPosition(1, targetPos);
		}
		if (targetFx != null)
		{
			targetFx.transform.position = targetPos;
		}
	}
}
