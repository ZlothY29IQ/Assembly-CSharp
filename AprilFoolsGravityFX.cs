using System.Collections;
using GorillaExtensions;
using GorillaTag.Gravity;
using UnityEngine;

public class AprilFoolsGravityFX : MonoBehaviour
{
	private void Start()
	{
		PersonalGravityZone personalGravityZone = base.gameObject.AddComponent<PersonalGravityZone>();
		MonkeGravityController component = GetComponent<MonkeGravityController>();
		personalGravityZone.AddTarget(component);
		component.SetPersonalGravityDirection(Random.insideUnitCircle.x0y().WithY(-0.5f).normalized);
		StartCoroutine(BackToNormal());
	}

	private IEnumerator BackToNormal()
	{
		yield return new WaitForSeconds(180f);
		PersonalGravityZone component = GetComponent<PersonalGravityZone>();
		MonkeGravityController component2 = GetComponent<MonkeGravityController>();
		component.RemoveTarget(component2);
	}
}
