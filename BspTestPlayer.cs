using TMPro;
using UnityEngine;

public class BspTestPlayer : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField]
	private float moveSpeed = 5f;

	[SerializeField]
	private bool use3DMovement;

	[Header("UI")]
	[SerializeField]
	private TextMeshPro zoneDisplayText;

	[SerializeField]
	private TextMeshPro positionDisplayText;

	[Header("BSP")]
	[SerializeField]
	private ZoneGraphBSP bspSystem;

	private string currentZoneName = "None";

	private ZoneDef currentZone;

	private void Start()
	{
		if (bspSystem == null)
		{
			bspSystem = Object.FindObjectOfType<ZoneGraphBSP>();
		}
		if (zoneDisplayText == null)
		{
			CreateUI();
		}
	}

	private void Update()
	{
		HandleMovement();
		UpdateZoneInfo();
		UpdateUI();
	}

	private void HandleMovement()
	{
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
		{
			zero.x -= 1f;
		}
		if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
		{
			zero.x += 1f;
		}
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
		{
			if (use3DMovement)
			{
				zero.z += 1f;
			}
			else
			{
				zero.y += 1f;
			}
		}
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
		{
			if (use3DMovement)
			{
				zero.z -= 1f;
			}
			else
			{
				zero.y -= 1f;
			}
		}
		if (use3DMovement)
		{
			if (Input.GetKey(KeyCode.Q))
			{
				zero.y += 1f;
			}
			if (Input.GetKey(KeyCode.E))
			{
				zero.y -= 1f;
			}
		}
		if (zero != Vector3.zero)
		{
			zero = zero.normalized * moveSpeed * Time.deltaTime;
			base.transform.position += zero;
		}
	}

	private void UpdateZoneInfo()
	{
		if (bspSystem == null)
		{
			return;
		}
		ZoneDef zoneDef = bspSystem.FindZoneAtPoint(base.transform.position);
		if (zoneDef != currentZone)
		{
			currentZone = zoneDef;
			currentZoneName = ((zoneDef != null) ? zoneDef.gameObject.name : "None");
			if (zoneDef != null)
			{
				Debug.Log("Player entered zone: " + currentZoneName);
			}
			else
			{
				Debug.Log("Player left all zones");
			}
		}
	}

	private void UpdateUI()
	{
		if (zoneDisplayText != null)
		{
			zoneDisplayText.text = "Current Zone: " + currentZoneName;
		}
		if (positionDisplayText != null)
		{
			Vector3 position = base.transform.position;
			positionDisplayText.text = $"Position: ({position.x:F1}, {position.y:F1}, {position.z:F1})";
		}
	}

	private void CreateUI()
	{
		GameObject gameObject = new GameObject("Zone Display");
		zoneDisplayText = gameObject.AddComponent<TextMeshPro>();
		zoneDisplayText.text = "Current Zone: None";
		zoneDisplayText.fontSize = 24f;
		zoneDisplayText.color = Color.white;
		zoneDisplayText.autoSizeTextContainer = true;
		gameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5f + Vector3.up * 3f + Vector3.left * 2f;
		gameObject.transform.LookAt(Camera.main.transform);
		gameObject.transform.Rotate(0f, 180f, 0f);
		GameObject gameObject2 = new GameObject("Position Display");
		positionDisplayText = gameObject2.AddComponent<TextMeshPro>();
		positionDisplayText.text = "Position: (0, 0, 0)";
		positionDisplayText.fontSize = 18f;
		positionDisplayText.color = Color.yellow;
		positionDisplayText.autoSizeTextContainer = true;
		gameObject2.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5f + Vector3.up * 2.5f + Vector3.left * 2f;
		gameObject2.transform.LookAt(Camera.main.transform);
		gameObject2.transform.Rotate(0f, 180f, 0f);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = ((currentZone != null) ? Color.green : Color.red);
		Gizmos.DrawWireSphere(base.transform.position, 0.5f);
		if (currentZone != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(base.transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
		}
		if (bspSystem != null && bspSystem.HasCompiledTree())
		{
			DrawBSPSplits();
		}
	}

	private void DrawBSPSplits()
	{
		SerializableBSPTree bSPTree = bspSystem.GetBSPTree();
		if (bSPTree?.nodes == null)
		{
			return;
		}
		BoxCollider[] array = Object.FindObjectsOfType<BoxCollider>();
		if (array.Length != 0)
		{
			Bounds bounds = new Bounds(array[0].bounds.center, array[0].bounds.size);
			BoxCollider[] array2 = array;
			foreach (BoxCollider boxCollider in array2)
			{
				bounds.Encapsulate(boxCollider.bounds);
			}
			bounds.Expand(2f);
			DrawPlayerPath(bSPTree, base.transform.position, bSPTree.rootIndex, bounds, 0);
		}
	}

	private void DrawPlayerPath(SerializableBSPTree tree, Vector3 playerPos, int nodeIndex, Bounds bounds, int depth)
	{
		if (nodeIndex >= tree.nodes.Length || depth >= tree.nodes.Length)
		{
			return;
		}
		SerializableBSPNode serializableBSPNode = tree.nodes[nodeIndex];
		Gizmos.color = GetAxisColor(serializableBSPNode.axis, depth);
		if (serializableBSPNode.axis == SerializableBSPNode.Axis.Zone)
		{
			Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
			Gizmos.DrawWireCube(bounds.center, bounds.size);
			return;
		}
		MatrixZonePair matrixZonePair;
		int num;
		Color color;
		if (serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixChain || serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixFinal)
		{
			if (serializableBSPNode.matrixIndex >= 0 && serializableBSPNode.matrixIndex < tree.matrices.Length)
			{
				matrixZonePair = tree.matrices[serializableBSPNode.matrixIndex];
				Vector3 vector = matrixZonePair.matrix.MultiplyPoint3x4(playerPos);
				if (Mathf.Abs(vector.x) <= 1f && Mathf.Abs(vector.y) <= 1f)
				{
					num = ((Mathf.Abs(vector.z) <= 1f) ? 1 : 0);
					if (num != 0)
					{
						color = Color.yellow;
						goto IL_0119;
					}
				}
				else
				{
					num = 0;
				}
				color = Color.red;
				goto IL_0119;
			}
			Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
			Gizmos.DrawWireCube(bounds.center, bounds.size);
			return;
		}
		DrawSplitPlane(serializableBSPNode.axis, serializableBSPNode.splitValue, bounds);
		Bounds bounds2 = bounds;
		Bounds bounds3 = bounds;
		switch (serializableBSPNode.axis)
		{
		case SerializableBSPNode.Axis.X:
			bounds2.SetMinMax(bounds.min, new Vector3(serializableBSPNode.splitValue, bounds.max.y, bounds.max.z));
			bounds3.SetMinMax(new Vector3(serializableBSPNode.splitValue, bounds.min.y, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Y:
			bounds2.SetMinMax(bounds.min, new Vector3(bounds.max.x, serializableBSPNode.splitValue, bounds.max.z));
			bounds3.SetMinMax(new Vector3(bounds.min.x, serializableBSPNode.splitValue, bounds.min.z), bounds.max);
			break;
		case SerializableBSPNode.Axis.Z:
			bounds2.SetMinMax(bounds.min, new Vector3(bounds.max.x, bounds.max.y, serializableBSPNode.splitValue));
			bounds3.SetMinMax(new Vector3(bounds.min.x, bounds.min.y, serializableBSPNode.splitValue), bounds.max);
			break;
		}
		if (GetAxisValue(playerPos, serializableBSPNode.axis) < serializableBSPNode.splitValue)
		{
			DrawPlayerPath(tree, playerPos, serializableBSPNode.leftChildIndex, bounds2, depth + 1);
		}
		else
		{
			DrawPlayerPath(tree, playerPos, serializableBSPNode.rightChildIndex, bounds3, depth + 1);
		}
		return;
		IL_0119:
		Gizmos.color = color;
		Matrix4x4 matrix = Gizmos.matrix;
		Gizmos.matrix = matrixZonePair.matrix.inverse;
		Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 2f);
		Gizmos.matrix = matrix;
		if (num == 0 && serializableBSPNode.axis == SerializableBSPNode.Axis.MatrixChain)
		{
			DrawPlayerPath(tree, playerPos, serializableBSPNode.outsideChildIndex, bounds, depth + 1);
		}
	}

	private float GetAxisValue(Vector3 point, SerializableBSPNode.Axis axis)
	{
		return axis switch
		{
			SerializableBSPNode.Axis.X => point.x, 
			SerializableBSPNode.Axis.Y => point.y, 
			SerializableBSPNode.Axis.Z => point.z, 
			SerializableBSPNode.Axis.MatrixChain => 0f, 
			SerializableBSPNode.Axis.MatrixFinal => 0f, 
			SerializableBSPNode.Axis.Zone => 0f, 
			_ => 0f, 
		};
	}

	private void DrawSplitPlane(SerializableBSPNode.Axis axis, float splitValue, Bounds bounds)
	{
		Vector3 center = bounds.center;
		_ = bounds.size;
		switch (axis)
		{
		case SerializableBSPNode.Axis.X:
			center.x = splitValue;
			Gizmos.DrawLine(new Vector3(splitValue, bounds.min.y, bounds.min.z), new Vector3(splitValue, bounds.max.y, bounds.max.z));
			Gizmos.DrawLine(new Vector3(splitValue, bounds.max.y, bounds.min.z), new Vector3(splitValue, bounds.min.y, bounds.max.z));
			break;
		case SerializableBSPNode.Axis.Y:
			center.y = splitValue;
			Gizmos.DrawLine(new Vector3(bounds.min.x, splitValue, bounds.min.z), new Vector3(bounds.max.x, splitValue, bounds.max.z));
			Gizmos.DrawLine(new Vector3(bounds.max.x, splitValue, bounds.min.z), new Vector3(bounds.min.x, splitValue, bounds.max.z));
			break;
		case SerializableBSPNode.Axis.Z:
			center.z = splitValue;
			Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.min.y, splitValue), new Vector3(bounds.max.x, bounds.max.y, splitValue));
			Gizmos.DrawLine(new Vector3(bounds.max.x, bounds.min.y, splitValue), new Vector3(bounds.min.x, bounds.max.y, splitValue));
			break;
		}
	}

	private Color GetAxisColor(SerializableBSPNode.Axis axis, int depth)
	{
		float a = 1f - (float)depth * 0.15f;
		a = Mathf.Max(a, 0.3f);
		return axis switch
		{
			SerializableBSPNode.Axis.X => new Color(1f, 0f, 0f, a), 
			SerializableBSPNode.Axis.Y => new Color(0f, 1f, 0f, a), 
			SerializableBSPNode.Axis.Z => new Color(0f, 0f, 1f, a), 
			SerializableBSPNode.Axis.MatrixChain => new Color(1f, 0f, 1f, a), 
			SerializableBSPNode.Axis.MatrixFinal => new Color(0.5f, 0f, 1f, a), 
			SerializableBSPNode.Axis.Zone => new Color(0f, 1f, 0f, a), 
			_ => new Color(1f, 1f, 1f, a), 
		};
	}
}
