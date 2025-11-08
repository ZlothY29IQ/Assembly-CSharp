using System.Collections.Generic;
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

	private List<Collider> colliders;

	private float timeSinceSeek;

	private float seekFreq;

	private void Start()
	{
		colliders = new List<Collider>();
		seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
	}

	private void Update()
	{
		timeSinceSeek += Time.deltaTime;
		if (timeSinceSeek > seekFreq)
		{
			seek();
			timeSinceSeek = 0f;
			seekFreq = rand.NextFloat(minseekFreq, maxseekFreq);
		}
	}

	private void seek()
	{
		float num = Mathf.Max(base.transform.lossyScale.x, base.transform.lossyScale.y, base.transform.lossyScale.z);
		colliders.Clear();
		colliders.AddRange(Physics.OverlapSphere(base.transform.position, maxRadias * num));
		Collider[] array = Physics.OverlapSphere(base.transform.position, minRadias * num);
		for (int i = 0; i < array.Length; i++)
		{
			colliders.Remove(array[i]);
		}
		if (colliders.Count > 0 && colliderFound != null)
		{
			colliderFound.Invoke(base.transform.position, colliders[rand.NextInt(colliders.Count)].transform.position);
		}
	}
}
