using UnityEngine;

public class RandomLocalColliders : MonoBehaviour
{
	private static SRand rand = new SRand("RandomLocalColliders");

	[SerializeField]
	private float minseekFreq = 3f;

	[SerializeField]
	private float maxseekFreq = 6f;

	[SerializeField]
	private float minRadias = 1f;

	[SerializeField]
	private float maxRadias = 10f;

	[SerializeField]
	private LightningDispatcherEvent colliderFound;

	private float timeSinceSeek;

	private float seekFreq;

	private RaycastHit[] raycastHits = new RaycastHit[100];

	private void Start()
	{
		seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
	}

	private void Update()
	{
		if (colliderFound != null)
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
		int num2 = Physics.RaycastNonAlloc(base.transform.position, rand.NextPointOnSphere(1f), raycastHits, maxRadias * num);
		if (num2 <= 0)
		{
			return;
		}
		int num3 = rand.NextInt(num2);
		for (int i = 0; i < num2; i++)
		{
			if (!(raycastHits[(i + num3) % num2].distance < minRadias * num))
			{
				colliderFound.Invoke(base.transform.position, raycastHits[(i + num3) % num2].point);
				break;
			}
		}
	}
}
