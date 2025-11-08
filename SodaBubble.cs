using System.Collections;
using UnityEngine;

public class SodaBubble : MonoBehaviour
{
	public MeshRenderer bubbleMesh;

	public Rigidbody body;

	public MeshCollider bubbleCollider;

	public AudioSource audioSource;

	public void Pop()
	{
		StartCoroutine(PopCoroutine());
	}

	private IEnumerator PopCoroutine()
	{
		audioSource.GTPlay();
		bubbleMesh.gameObject.SetActive(value: false);
		bubbleCollider.gameObject.SetActive(value: false);
		yield return new WaitForSeconds(1f);
		bubbleMesh.gameObject.SetActive(value: true);
		bubbleCollider.gameObject.SetActive(value: true);
		ObjectPools.instance.Destroy(base.gameObject);
	}
}
