using System.Collections;
using UnityEngine;

public class D20_ShaderManager : MonoBehaviour
{
	private Rigidbody rb;

	private Vector3 lastPosition;

	public float updateInterval = 0.1f;

	public Vector3 velocity;

	private Material material;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		lastPosition = base.transform.position;
		Renderer component = GetComponent<Renderer>();
		material = component.material;
		material.SetVector("_Velocity", velocity);
		StartCoroutine(UpdateVelocityCoroutine());
	}

	private IEnumerator UpdateVelocityCoroutine()
	{
		while (true)
		{
			Vector3 position = base.transform.position;
			velocity = (position - lastPosition) / updateInterval;
			lastPosition = position;
			material.SetVector("_Velocity", velocity);
			yield return new WaitForSeconds(updateInterval);
		}
	}
}
