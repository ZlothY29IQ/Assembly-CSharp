using System.Collections.Generic;
using System.Diagnostics;
using Drawing;
using UnityEngine;

public static class GizmoUtils
{
	private static readonly Dictionary<Collider, Color> gColliderToColor = new Dictionary<Collider, Color>(64);

	[Conditional("UNITY_EDITOR")]
	public static void DrawGizmo(this Collider c, Color color = default(Color))
	{
		if (c == null)
		{
			return;
		}
		if (color == default(Color) && !gColliderToColor.TryGetValue(c, out color))
		{
			color = new SRand(c.GetHashCode()).NextColor();
			gColliderToColor.Add(c, color);
		}
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithMatrix(c.transform.localToWorldMatrix))
		{
			if (!(c is BoxCollider boxCollider))
			{
				if (!(c is SphereCollider sphereCollider))
				{
					if (!(c is CapsuleCollider capsuleCollider))
					{
						if (c is MeshCollider meshCollider)
						{
							ingame.WireMesh(meshCollider.sharedMesh, color);
						}
					}
					else
					{
						ingame.WireCapsule(capsuleCollider.center, Vector3.up, capsuleCollider.height, capsuleCollider.radius, color);
					}
				}
				else
				{
					ingame.WireSphere(sphereCollider.center, sphereCollider.radius, color);
				}
			}
			else
			{
				ingame.WireBox(boxCollider.center, boxCollider.size, color);
			}
		}
	}

	[Conditional("UNITY_EDITOR")]
	public static void DrawWireCubeTRS(Vector3 t, Quaternion r, Vector3 s)
	{
	}
}
