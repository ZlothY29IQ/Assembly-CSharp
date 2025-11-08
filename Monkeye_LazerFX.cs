using UnityEngine;

public class Monkeye_LazerFX : MonoBehaviour
{
	private Transform[] eyeBones;

	private VRRig rig;

	public LineRenderer[] lines;

	private void Awake()
	{
		base.enabled = false;
	}

	public void EnableLazer(Transform[] eyes_, VRRig rig_)
	{
		if (!(rig_ == rig))
		{
			eyeBones = eyes_;
			rig = rig_;
			base.enabled = true;
			LineRenderer[] array = lines;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].positionCount = 2;
			}
		}
	}

	public void DisableLazer()
	{
		if (base.enabled)
		{
			base.enabled = false;
			LineRenderer[] array = lines;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].positionCount = 0;
			}
		}
	}

	private void Update()
	{
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i].SetPosition(0, eyeBones[i].transform.position);
			lines[i].SetPosition(1, rig.transform.position);
		}
	}
}
