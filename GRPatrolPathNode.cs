using UnityEngine;

public class GRPatrolPathNode : MonoBehaviour
{
	public void OnDrawGizmosSelected()
	{
		if (!(base.transform.parent == null))
		{
			GRPatrolPath component = base.transform.parent.GetComponent<GRPatrolPath>();
			if (!(component == null))
			{
				component.OnDrawGizmosSelected();
			}
		}
	}
}
