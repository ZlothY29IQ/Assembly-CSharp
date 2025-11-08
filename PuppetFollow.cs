using UnityEngine;

public class PuppetFollow : MonoBehaviour
{
	public Transform sourceTarget;

	public Transform sourceBase;

	public Transform puppetBase;

	private void FixedUpdate()
	{
		base.transform.position = sourceTarget.position - sourceBase.position + puppetBase.position;
		base.transform.localRotation = sourceTarget.localRotation;
	}
}
