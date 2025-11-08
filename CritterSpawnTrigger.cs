using Sirenix.OdinInspector;
using UnityEngine;

public class CritterSpawnTrigger : MonoBehaviour
{
	[Header("Trigger Settings")]
	[SerializeField]
	private CrittersActor.CrittersActorType triggerActorType;

	[SerializeField]
	private int requiredSubObjectIndex = -1;

	[SerializeField]
	private string triggerActorName;

	[SerializeField]
	private float triggerCooldown = 1f;

	[Header("Spawn Settings")]
	[SerializeField]
	private Transform spawnPoint;

	[SerializeField]
	private int critterType;

	private float _nextSpawnTime;

	private ValueDropdownList<int> GetCritterTypeList()
	{
		return new ValueDropdownList<int>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (CrittersManager.instance.LocalAuthority() && !(Time.realtimeSinceStartup < _nextSpawnTime))
		{
			CrittersActor componentInParent = other.GetComponentInParent<CrittersActor>();
			if ((bool)componentInParent && componentInParent.crittersActorType == triggerActorType && (requiredSubObjectIndex < 0 || componentInParent.subObjectIndex == requiredSubObjectIndex) && (string.IsNullOrEmpty(triggerActorName) || componentInParent.GetActorSubtype().Contains(triggerActorName)))
			{
				CrittersManager.instance.DespawnActor(componentInParent);
				CrittersManager.instance.SpawnCritter(critterType, spawnPoint.position, spawnPoint.rotation);
				_nextSpawnTime = Time.realtimeSinceStartup + triggerCooldown;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawLine(base.transform.position, spawnPoint.position);
		Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
	}
}
