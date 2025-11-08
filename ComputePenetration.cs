using Drawing;
using UnityEngine;

public class ComputePenetration : MonoBehaviour
{
	public Collider colliderA;

	public Collider colliderB;

	public bool overlapped;

	public Vector3 direction;

	public float distance;

	private TimeSince lastUpdate = TimeSince.Now();

	public void Compute()
	{
		if (!(colliderA == null))
		{
			_ = colliderB == null;
		}
	}

	public void OnDrawGizmos()
	{
		if (!(colliderA.AsNull() == null) && !(colliderB.AsNull() == null))
		{
			Transform transform = colliderA.transform;
			Transform transform2 = colliderB.transform;
			if (lastUpdate.HasElapsed(0.5f, resetOnElapsed: true))
			{
				overlapped = Physics.ComputePenetration(colliderA, transform.position, transform.rotation, colliderB, transform2.position, transform2.rotation, out direction, out distance);
			}
			Color color = (overlapped ? Color.red : Color.green);
			DrawCollider(colliderA, color);
			DrawCollider(colliderB, color);
			if (overlapped)
			{
				Vector3 position = colliderB.transform.position;
				Vector3 to = position + direction * distance;
				Gizmos.DrawLine(position, to);
			}
		}
	}

	private void DrawCollider(Collider c, Color color)
	{
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithMatrix(c.transform.localToWorldMatrix))
		{
			ingame.PushColor(color);
			if (!(c is BoxCollider boxCollider))
			{
				if (!(c is SphereCollider sphereCollider))
				{
					if (c is CapsuleCollider capsuleCollider)
					{
						ingame.WireCapsule(capsuleCollider.center, Vector3.up, capsuleCollider.height, capsuleCollider.radius);
					}
				}
				else
				{
					ingame.WireSphere(sphereCollider.center, sphereCollider.radius);
				}
			}
			else
			{
				ingame.WireBox(boxCollider.center, boxCollider.size);
			}
			ingame.PopColor();
		}
	}
}
