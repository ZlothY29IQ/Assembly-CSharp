using UnityEngine;

public class RadialGravity : MonoBehaviour
{
	private void Start()
	{
		Rigidbody component = GetComponent<Rigidbody>();
		if (!(component == null))
		{
			component.centerOfMass = new Vector3(0f, -3f, 0f);
			base.transform.localScale = Vector3.one * Random.Range(1f, 3f);
			component.useGravity = false;
			ConstantForce constantForce = base.gameObject.AddComponent<ConstantForce>();
			constantForce.force = Random.onUnitSphere * Physics.gravity.magnitude;
			if (Vector3.Dot(constantForce.force, Vector3.up) > 0f && Random.value > 0.33f)
			{
				constantForce.force = Random.onUnitSphere * Physics.gravity.magnitude;
			}
		}
	}
}
