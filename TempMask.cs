using System;
using System.Collections;
using GorillaNetworking;
using UnityEngine;

public class TempMask : MonoBehaviour
{
	public int year;

	public int month;

	public int day;

	public DateTime dayOn;

	public MeshRenderer myRenderer;

	private DateTime myDate;

	private VRRig myRig;

	private void Awake()
	{
		dayOn = new DateTime(year, month, day);
		myRig = GetComponentInParent<VRRig>();
		if (myRig != null && myRig.netView.IsMine && !myRig.isOfflineVRRig)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void OnEnable()
	{
		StartCoroutine(MaskOnDuringDate());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private IEnumerator MaskOnDuringDate()
	{
		while (true)
		{
			if (GorillaComputer.instance != null && GorillaComputer.instance.startupMillis != 0L)
			{
				myDate = new DateTime(GorillaComputer.instance.startupMillis * 10000 + (long)(Time.realtimeSinceStartup * 1000f * 10000f)).Subtract(TimeSpan.FromHours(7.0));
				if (myDate.DayOfYear == dayOn.DayOfYear)
				{
					if (!myRenderer.enabled)
					{
						myRenderer.enabled = true;
					}
				}
				else if (myRenderer.enabled)
				{
					myRenderer.enabled = false;
				}
			}
			yield return new WaitForSeconds(1f);
		}
	}
}
