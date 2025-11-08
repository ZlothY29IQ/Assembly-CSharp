using NetSynchrony;
using UnityEngine;

public class ReportTargetHit : MonoBehaviour
{
	private static SRand rand = new SRand("ReportForwardHit");

	[SerializeField]
	private float minseekFreq = 3f;

	[SerializeField]
	private float maxseekFreq = 6f;

	[SerializeField]
	private Transform[] targets;

	[SerializeField]
	private LightningDispatcherEvent colliderFound;

	private float timeSinceSeek;

	private float seekFreq;

	[SerializeField]
	private RandomDispatcher nsRand;

	private void Start()
	{
		seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
	}

	private void OnEnable()
	{
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
		if (targets.Length != 0)
		{
			Vector3 direction = targets[rand.NextInt(targets.Length)].position - base.transform.position;
			if (Physics.Raycast(base.transform.position, direction, out var hitInfo) && colliderFound != null)
			{
				colliderFound.Invoke(base.transform.position, hitInfo.point);
			}
		}
	}
}
