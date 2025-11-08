using NetSynchrony;
using UnityEngine;

public class ReportForwardHit : MonoBehaviour
{
	private static SRand rand = new SRand("ReportForwardHit");

	[SerializeField]
	private float minseekFreq = 3f;

	[SerializeField]
	private float maxseekFreq = 6f;

	[SerializeField]
	private float maxRadias = 10f;

	[SerializeField]
	private LightningDispatcherEvent colliderFound;

	[SerializeField]
	private RandomDispatcher nsRand;

	private float timeSinceSeek;

	private float seekFreq;

	[SerializeField]
	private bool seekOnEnable;

	private void Start()
	{
		seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
	}

	private void OnEnable()
	{
		if (seekOnEnable)
		{
			seek();
		}
		if (nsRand != null)
		{
			nsRand.Dispatch += NsRand_Dispatch;
		}
	}

	private void OnDisable()
	{
		if (nsRand != null)
		{
			nsRand.Dispatch -= NsRand_Dispatch;
		}
	}

	private void NsRand_Dispatch(RandomDispatcher randomDispatcher)
	{
		seek();
	}

	private void Update()
	{
		if (!(nsRand != null))
		{
			timeSinceSeek += Time.deltaTime;
			if (timeSinceSeek > seekFreq)
			{
				seek();
				timeSinceSeek = 0f;
				seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
			}
		}
	}

	private void seek()
	{
		float num = Mathf.Max(base.transform.lossyScale.x, base.transform.lossyScale.y, base.transform.lossyScale.z);
		if (Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, maxRadias * num) && colliderFound != null)
		{
			colliderFound.Invoke(base.transform.position, hitInfo.point);
		}
	}
}
