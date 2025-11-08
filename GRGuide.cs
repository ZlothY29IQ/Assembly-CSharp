using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GRGuide : MonoBehaviourTick
{
	public Transform tempTarget;

	public List<GameObject> show;

	public AudioSource audioSource;

	private bool showing;

	private bool hasPath;

	private NavMeshPath path;

	private int numPathCorners;

	private Vector3[] pathCorners;

	private List<Vector3> connectorCorners;

	private void Awake()
	{
		path = new NavMeshPath();
		showing = false;
		for (int i = 0; i < show.Count; i++)
		{
			show[i].SetActive(value: false);
		}
		hasPath = false;
		numPathCorners = 0;
		pathCorners = new Vector3[512];
		connectorCorners = new List<Vector3>(64);
	}

	public override void Tick()
	{
		bool flag = GRPlayer.Get(VRRig.LocalRig).State == GRPlayer.GRPlayerState.Ghost;
		Vector3 position = VRRig.LocalRig.transform.position;
		float sqrMagnitude = (position - base.transform.position).sqrMagnitude;
		if (flag && (!hasPath || sqrMagnitude > 36f))
		{
			hasPath = false;
			if (GhostReactor.instance.levelGenerator.GetExitFromCurrentSection(position, out var exitPos, out var _, connectorCorners) && NavMesh.SamplePosition(position, out var hit, 5f, -1) && NavMesh.SamplePosition(exitPos, out var hit2, 5f, -1) && NavMesh.CalculatePath(hit.position, hit2.position, -1, path) && path.status == NavMeshPathStatus.PathComplete)
			{
				numPathCorners = path.GetCornersNonAlloc(pathCorners);
				for (int num = connectorCorners.Count - 1; num >= 0; num--)
				{
					pathCorners[numPathCorners] = connectorCorners[num];
					numPathCorners++;
				}
				if (numPathCorners > 0)
				{
					base.transform.position = pathCorners[0];
					hasPath = true;
				}
			}
		}
		if (!flag)
		{
			hasPath = false;
		}
		if (showing != hasPath)
		{
			showing = hasPath;
			for (int i = 0; i < show.Count; i++)
			{
				show[i].SetActive(showing);
			}
			if (audioSource != null)
			{
				if (showing)
				{
					audioSource.Play();
				}
				else
				{
					audioSource.Stop();
				}
			}
		}
		if (!hasPath)
		{
			return;
		}
		int nextCorner;
		Vector3 closestPointOnPath = GetClosestPointOnPath(position, pathCorners, numPathCorners, out nextCorner);
		float num2 = 2.5f;
		Vector3 vector = closestPointOnPath;
		for (int j = nextCorner; j < numPathCorners; j++)
		{
			Vector3 vector2 = pathCorners[j] - vector;
			float magnitude = vector2.magnitude;
			if (num2 <= magnitude)
			{
				vector += vector2 * (num2 / magnitude);
				break;
			}
			num2 -= magnitude;
			vector = pathCorners[j];
		}
		base.transform.position = vector;
	}

	private static Vector3 GetClosestPointOnPath(Vector3 pos, Vector3[] pathCorners, int numPathCorners, out int nextCorner)
	{
		nextCorner = 0;
		switch (numPathCorners)
		{
		case 0:
			return pos;
		case 1:
			return pathCorners[0];
		default:
		{
			float num = float.MaxValue;
			Vector3 result = Vector3.zero;
			for (int i = 0; i < numPathCorners - 1; i++)
			{
				Vector3 vA = pathCorners[i];
				Vector3 vB = pathCorners[i + 1];
				Vector3 vector = ClosestPointOnLine(vA, vB, pos);
				float sqrMagnitude = (vector - pos).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = vector;
					nextCorner = i + 1;
				}
			}
			return result;
		}
		}
	}

	public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
	{
		Vector3 rhs = vPoint - vA;
		Vector3 normalized = (vB - vA).normalized;
		float num = Vector3.Distance(vA, vB);
		float num2 = Vector3.Dot(normalized, rhs);
		if (num2 <= 0f)
		{
			return vA;
		}
		if (num2 >= num)
		{
			return vB;
		}
		Vector3 vector = normalized * num2;
		return vA + vector;
	}
}
