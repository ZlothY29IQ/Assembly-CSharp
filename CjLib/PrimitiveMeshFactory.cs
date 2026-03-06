using System;
using System.Collections.Generic;
using UnityEngine;

namespace CjLib;

public class PrimitiveMeshFactory
{
	private static int s_lastDrawLineFrame = -1;

	private static int s_iPooledMesh = 0;

	private static List<Mesh> s_lineMeshPool;

	private static Mesh s_boxWireframeMesh;

	private static Mesh s_boxSolidColorMesh;

	private static Mesh s_boxFlatShadedMesh;

	private static Mesh s_rectWireframeMesh;

	private static Mesh s_rectSolidColorMesh;

	private static Mesh s_rectFlatShadedMesh;

	private static Dictionary<int, Mesh> s_circleWireframeMeshPool;

	private static Dictionary<int, Mesh> s_circleSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_circleFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_cylinderWireframeMeshPool;

	private static Dictionary<int, Mesh> s_cylinderSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_cylinderFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_cylinderSmoothShadedMeshPool;

	private static Dictionary<int, Mesh> s_sphereWireframeMeshPool;

	private static Dictionary<int, Mesh> s_sphereSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_sphereFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_sphereSmoothShadedMeshPool;

	private static Dictionary<int, Mesh> s_capsuleWireframeMeshPool;

	private static Dictionary<int, Mesh> s_capsuleSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_capsuleFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_capsuleSmoothShadedMeshPool;

	private static Dictionary<int, Mesh> s_capsule2dWireframeMeshPool;

	private static Dictionary<int, Mesh> s_capsule2dSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_capsule2dFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_coneWireframeMeshPool;

	private static Dictionary<int, Mesh> s_coneSolidColorMeshPool;

	private static Dictionary<int, Mesh> s_coneFlatShadedMeshPool;

	private static Dictionary<int, Mesh> s_coneSmoothhadedMeshPool;

	private static Mesh GetPooledLineMesh()
	{
		if (s_lineMeshPool == null)
		{
			s_lineMeshPool = new List<Mesh>();
			s_lineMeshPool.Add(new Mesh());
		}
		if (s_lastDrawLineFrame != Time.frameCount)
		{
			s_iPooledMesh = 0;
			s_lastDrawLineFrame = Time.frameCount;
		}
		if (s_iPooledMesh == s_lineMeshPool.Count)
		{
			s_lineMeshPool.Capacity *= 2;
			for (int i = s_iPooledMesh; i < s_lineMeshPool.Capacity; i++)
			{
				s_lineMeshPool.Add(new Mesh());
			}
		}
		Mesh mesh = s_lineMeshPool[s_iPooledMesh++];
		if (mesh == null)
		{
			Mesh mesh2 = (s_lineMeshPool[s_iPooledMesh - 1] = new Mesh());
			mesh = mesh2;
		}
		return mesh;
	}

	public static Mesh Line(Vector3 v0, Vector3 v1)
	{
		Mesh pooledLineMesh = GetPooledLineMesh();
		if (pooledLineMesh == null)
		{
			return null;
		}
		Vector3[] vertices = new Vector3[2] { v0, v1 };
		int[] indices = new int[2] { 0, 1 };
		pooledLineMesh.vertices = vertices;
		pooledLineMesh.SetIndices(indices, MeshTopology.Lines, 0);
		pooledLineMesh.RecalculateBounds();
		return pooledLineMesh;
	}

	public static Mesh Lines(Vector3[] aVert)
	{
		if (aVert.Length <= 1)
		{
			return null;
		}
		Mesh pooledLineMesh = GetPooledLineMesh();
		if (pooledLineMesh == null)
		{
			return null;
		}
		int[] array = new int[aVert.Length];
		for (int i = 0; i < aVert.Length; i++)
		{
			array[i] = i;
		}
		pooledLineMesh.vertices = aVert;
		pooledLineMesh.SetIndices(array, MeshTopology.Lines, 0);
		return pooledLineMesh;
	}

	public static Mesh LineStrip(Vector3[] aVert)
	{
		if (aVert.Length <= 1)
		{
			return null;
		}
		Mesh pooledLineMesh = GetPooledLineMesh();
		if (pooledLineMesh == null)
		{
			return null;
		}
		int[] array = new int[aVert.Length];
		for (int i = 0; i < aVert.Length; i++)
		{
			array[i] = i;
		}
		pooledLineMesh.vertices = aVert;
		pooledLineMesh.SetIndices(array, MeshTopology.LineStrip, 0);
		return pooledLineMesh;
	}

	public static Mesh BoxWireframe()
	{
		if (s_boxWireframeMesh == null)
		{
			s_boxWireframeMesh = new Mesh();
			Vector3[] array = new Vector3[8]
			{
				new Vector3(-0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, -0.5f, 0.5f),
				new Vector3(-0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, -0.5f, 0.5f)
			};
			int[] indices = new int[26]
			{
				0, 1, 1, 2, 2, 3, 3, 0, 2, 6,
				6, 7, 7, 3, 7, 4, 4, 5, 5, 6,
				5, 1, 1, 0, 0, 4
			};
			s_boxWireframeMesh.vertices = array;
			s_boxWireframeMesh.normals = array;
			s_boxWireframeMesh.SetIndices(indices, MeshTopology.Lines, 0);
		}
		return s_boxWireframeMesh;
	}

	public static Mesh BoxSolidColor()
	{
		if (s_boxSolidColorMesh == null)
		{
			s_boxSolidColorMesh = new Mesh();
			Vector3[] vertices = new Vector3[8]
			{
				new Vector3(-0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, -0.5f, 0.5f),
				new Vector3(-0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, -0.5f, 0.5f)
			};
			int[] indices = new int[36]
			{
				0, 1, 2, 0, 2, 3, 3, 2, 6, 3,
				6, 7, 7, 6, 5, 7, 5, 4, 4, 5,
				1, 4, 1, 0, 1, 5, 6, 1, 6, 2,
				0, 3, 7, 0, 7, 4
			};
			s_boxSolidColorMesh.vertices = vertices;
			s_boxSolidColorMesh.SetIndices(indices, MeshTopology.Triangles, 0);
		}
		return s_boxSolidColorMesh;
	}

	public static Mesh BoxFlatShaded()
	{
		if (s_boxFlatShadedMesh == null)
		{
			s_boxFlatShadedMesh = new Mesh();
			Vector3[] array = new Vector3[8]
			{
				new Vector3(-0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, 0.5f, -0.5f),
				new Vector3(0.5f, -0.5f, -0.5f),
				new Vector3(-0.5f, -0.5f, 0.5f),
				new Vector3(-0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, 0.5f, 0.5f),
				new Vector3(0.5f, -0.5f, 0.5f)
			};
			Vector3[] array2 = new Vector3[36]
			{
				array[0],
				array[1],
				array[2],
				array[0],
				array[2],
				array[3],
				array[3],
				array[2],
				array[6],
				array[3],
				array[6],
				array[7],
				array[7],
				array[6],
				array[5],
				array[7],
				array[5],
				array[4],
				array[4],
				array[5],
				array[1],
				array[4],
				array[1],
				array[0],
				array[1],
				array[5],
				array[6],
				array[1],
				array[6],
				array[2],
				array[0],
				array[3],
				array[7],
				array[0],
				array[7],
				array[4]
			};
			Vector3[] array3 = new Vector3[6]
			{
				new Vector3(0f, 0f, -1f),
				new Vector3(1f, 0f, 0f),
				new Vector3(0f, 0f, 1f),
				new Vector3(-1f, 0f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, -1f, 0f)
			};
			Vector3[] normals = new Vector3[36]
			{
				array3[0],
				array3[0],
				array3[0],
				array3[0],
				array3[0],
				array3[0],
				array3[1],
				array3[1],
				array3[1],
				array3[1],
				array3[1],
				array3[1],
				array3[2],
				array3[2],
				array3[2],
				array3[2],
				array3[2],
				array3[2],
				array3[3],
				array3[3],
				array3[3],
				array3[3],
				array3[3],
				array3[3],
				array3[4],
				array3[4],
				array3[4],
				array3[4],
				array3[4],
				array3[4],
				array3[5],
				array3[5],
				array3[5],
				array3[5],
				array3[5],
				array3[5]
			};
			int[] array4 = new int[array2.Length];
			for (int i = 0; i < array4.Length; i++)
			{
				array4[i] = i;
			}
			s_boxFlatShadedMesh.vertices = array2;
			s_boxFlatShadedMesh.normals = normals;
			s_boxFlatShadedMesh.SetIndices(array4, MeshTopology.Triangles, 0);
		}
		return s_boxFlatShadedMesh;
	}

	public static Mesh RectWireframe()
	{
		if (s_rectWireframeMesh == null)
		{
			s_rectWireframeMesh = new Mesh();
			Vector3[] array = new Vector3[4]
			{
				new Vector3(-0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, -0.5f)
			};
			int[] indices = new int[8] { 0, 1, 1, 2, 2, 3, 3, 0 };
			s_rectWireframeMesh.vertices = array;
			s_rectWireframeMesh.normals = array;
			s_rectWireframeMesh.SetIndices(indices, MeshTopology.Lines, 0);
		}
		return s_rectWireframeMesh;
	}

	public static Mesh RectSolidColor()
	{
		if (s_rectSolidColorMesh == null)
		{
			s_rectSolidColorMesh = new Mesh();
			Vector3[] vertices = new Vector3[4]
			{
				new Vector3(-0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, -0.5f)
			};
			int[] indices = new int[12]
			{
				0, 1, 2, 0, 2, 3, 0, 2, 1, 0,
				3, 2
			};
			s_rectSolidColorMesh.vertices = vertices;
			s_rectSolidColorMesh.SetIndices(indices, MeshTopology.Triangles, 0);
		}
		return s_rectSolidColorMesh;
	}

	public static Mesh RectFlatShaded()
	{
		if (s_rectFlatShadedMesh == null)
		{
			s_rectFlatShadedMesh = new Mesh();
			Vector3[] vertices = new Vector3[8]
			{
				new Vector3(-0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, 0.5f),
				new Vector3(0.5f, 0f, -0.5f)
			};
			Vector3[] normals = new Vector3[8]
			{
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(0f, -1f, 0f),
				new Vector3(0f, -1f, 0f),
				new Vector3(0f, -1f, 0f),
				new Vector3(0f, -1f, 0f)
			};
			int[] indices = new int[12]
			{
				0, 1, 2, 0, 2, 3, 4, 6, 5, 4,
				7, 6
			};
			s_rectFlatShadedMesh.vertices = vertices;
			s_rectFlatShadedMesh.normals = normals;
			s_rectFlatShadedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
		}
		return s_rectFlatShadedMesh;
	}

	public static Mesh CircleWireframe(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_circleWireframeMeshPool == null)
		{
			s_circleWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_circleWireframeMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments];
			int[] array2 = new int[numSegments + 1];
			float num = MathF.PI * 2f / (float)numSegments;
			float num2 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array[i] = Mathf.Cos(num2) * Vector3.right + Mathf.Sin(num2) * Vector3.forward;
				array2[i] = i;
				num2 += num;
			}
			array2[numSegments] = 0;
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.LineStrip, 0);
			if (s_circleWireframeMeshPool.ContainsKey(numSegments))
			{
				s_circleWireframeMeshPool.Remove(numSegments);
			}
			s_circleWireframeMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CircleSolidColor(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_circleSolidColorMeshPool == null)
		{
			s_circleSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_circleSolidColorMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments + 1];
			int[] array2 = new int[numSegments * 6];
			int num = 0;
			float num2 = MathF.PI * 2f / (float)numSegments;
			float num3 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array[i] = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				num3 += num2;
				array2[num++] = numSegments;
				array2[num++] = (i + 1) % numSegments;
				array2[num++] = i;
				array2[num++] = numSegments;
				array2[num++] = i;
				array2[num++] = (i + 1) % numSegments;
			}
			array[numSegments] = Vector3.zero;
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_circleSolidColorMeshPool.ContainsKey(numSegments))
			{
				s_circleSolidColorMeshPool.Remove(numSegments);
			}
			s_circleSolidColorMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CircleFlatShaded(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_circleFlatShadedMeshPool == null)
		{
			s_circleFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_circleFlatShadedMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[(numSegments + 1) * 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[numSegments * 6];
			int num = 0;
			float num2 = MathF.PI * 2f / (float)numSegments;
			float num3 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array[i] = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				num3 += num2;
				array2[i] = new Vector3(0f, 1f, 0f);
				array3[num++] = numSegments;
				array3[num++] = (i + 1) % numSegments;
				array3[num++] = i;
			}
			array[numSegments] = Vector3.zero;
			array2[numSegments] = new Vector3(0f, 1f, 0f);
			num3 = 0f;
			for (int j = 0; j < numSegments; j++)
			{
				array[j + numSegments + 1] = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				num3 -= num2;
				array2[j + numSegments + 1] = new Vector3(0f, -1f, 0f);
				array3[num++] = numSegments * 2 + 1;
				array3[num++] = (j + 1) % numSegments + numSegments + 1;
				array3[num++] = j + (numSegments + 1);
			}
			array[numSegments * 2 + 1] = Vector3.zero;
			array2[numSegments * 2 + 1] = new Vector3(0f, -1f, 0f);
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_circleFlatShadedMeshPool.ContainsKey(numSegments))
			{
				s_circleFlatShadedMeshPool.Remove(numSegments);
			}
			s_circleFlatShadedMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CylinderWireframe(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_cylinderWireframeMeshPool == null)
		{
			s_cylinderWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_cylinderWireframeMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 2];
			int[] array2 = new int[numSegments * 6];
			Vector3 vector = new Vector3(0f, -0.5f, 0f);
			Vector3 vector2 = new Vector3(0f, 0.5f, 0f);
			int num = 0;
			float num2 = MathF.PI * 2f / (float)numSegments;
			float num3 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				Vector3 vector3 = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				array[i] = vector + vector3;
				array[numSegments + i] = vector2 + vector3;
				array2[num++] = i;
				array2[num++] = (i + 1) % numSegments;
				array2[num++] = i;
				array2[num++] = numSegments + i;
				array2[num++] = numSegments + i;
				array2[num++] = numSegments + (i + 1) % numSegments;
				num3 += num2;
			}
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.Lines, 0);
			if (s_cylinderWireframeMeshPool.ContainsKey(numSegments))
			{
				s_cylinderWireframeMeshPool.Remove(numSegments);
			}
			s_cylinderWireframeMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CylinderSolidColor(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_cylinderSolidColorMeshPool == null)
		{
			s_cylinderSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_cylinderSolidColorMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 2 + 2];
			int[] array2 = new int[numSegments * 12];
			Vector3 vector = new Vector3(0f, -0.5f, 0f);
			Vector3 vector2 = new Vector3(0f, 0.5f, 0f);
			int num = 0;
			int num2 = numSegments * 2;
			int num3 = numSegments * 2 + 1;
			array[num2] = vector;
			array[num3] = vector2;
			int num4 = 0;
			float num5 = MathF.PI * 2f / (float)numSegments;
			float num6 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				Vector3 vector3 = Mathf.Cos(num6) * Vector3.right + Mathf.Sin(num6) * Vector3.forward;
				array[num + i] = vector + vector3;
				array[numSegments + i] = vector2 + vector3;
				array2[num4++] = num2;
				array2[num4++] = num + i;
				array2[num4++] = num + (i + 1) % numSegments;
				array2[num4++] = num + i;
				array2[num4++] = numSegments + (i + 1) % numSegments;
				array2[num4++] = num + (i + 1) % numSegments;
				array2[num4++] = num + i;
				array2[num4++] = numSegments + i;
				array2[num4++] = numSegments + (i + 1) % numSegments;
				array2[num4++] = num3;
				array2[num4++] = numSegments + (i + 1) % numSegments;
				array2[num4++] = numSegments + i;
				num6 += num5;
			}
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_cylinderSolidColorMeshPool.ContainsKey(numSegments))
			{
				s_cylinderSolidColorMeshPool.Remove(numSegments);
			}
			s_cylinderSolidColorMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CylinderFlatShaded(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_cylinderFlatShadedMeshPool == null)
		{
			s_cylinderFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_cylinderFlatShadedMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 6 + 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[numSegments * 12];
			Vector3 vector = new Vector3(0f, -0.5f, 0f);
			Vector3 vector2 = new Vector3(0f, 0.5f, 0f);
			int num = 0;
			int num2 = numSegments * 2;
			int num3 = numSegments * 6;
			int num4 = numSegments * 6 + 1;
			array[num3] = vector;
			array[num4] = vector2;
			array2[num3] = new Vector3(0f, -1f, 0f);
			array2[num4] = new Vector3(0f, 1f, 0f);
			int num5 = 0;
			float num6 = MathF.PI * 2f / (float)numSegments;
			float num7 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				Vector3 vector3 = Mathf.Cos(num7) * Vector3.right + Mathf.Sin(num7) * Vector3.forward;
				array[num + i] = vector + vector3;
				array[numSegments + i] = vector2 + vector3;
				array2[num + i] = new Vector3(0f, -1f, 0f);
				array2[numSegments + i] = new Vector3(0f, 1f, 0f);
				array3[num5++] = num3;
				array3[num5++] = num + i;
				array3[num5++] = num + (i + 1) % numSegments;
				array3[num5++] = num4;
				array3[num5++] = numSegments + (i + 1) % numSegments;
				array3[num5++] = numSegments + i;
				num7 += num6;
				Vector3 vector4 = Mathf.Cos(num7) * Vector3.right + Mathf.Sin(num7) * Vector3.forward;
				array[num2 + i * 4] = vector + vector3;
				array[num2 + i * 4 + 1] = vector2 + vector3;
				array[num2 + i * 4 + 2] = vector + vector4;
				array[num2 + i * 4 + 3] = vector2 + vector4;
				array2[num2 + i * 4 + 3] = (array2[num2 + i * 4 + 2] = (array2[num2 + i * 4 + 1] = (array2[num2 + i * 4] = Vector3.Cross(vector2 - vector, vector4 - vector3).normalized)));
				array3[num5++] = num2 + i * 4;
				array3[num5++] = num2 + i * 4 + 3;
				array3[num5++] = num2 + i * 4 + 2;
				array3[num5++] = num2 + i * 4;
				array3[num5++] = num2 + i * 4 + 1;
				array3[num5++] = num2 + i * 4 + 3;
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_cylinderFlatShadedMeshPool.ContainsKey(numSegments))
			{
				s_cylinderFlatShadedMeshPool.Remove(numSegments);
			}
			s_cylinderFlatShadedMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh CylinderSmoothShaded(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_cylinderSmoothShadedMeshPool == null)
		{
			s_cylinderSmoothShadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_cylinderSmoothShadedMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 4 + 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[numSegments * 12];
			Vector3 vector = new Vector3(0f, -0.5f, 0f);
			Vector3 vector2 = new Vector3(0f, 0.5f, 0f);
			int num = 0;
			int num2 = numSegments * 2;
			int num3 = numSegments * 4;
			int num4 = numSegments * 4 + 1;
			array[num3] = vector;
			array[num4] = vector2;
			array2[num3] = new Vector3(0f, -1f, 0f);
			array2[num4] = new Vector3(0f, 1f, 0f);
			int num5 = 0;
			float num6 = MathF.PI * 2f / (float)numSegments;
			float num7 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				Vector3 vector3 = Mathf.Cos(num7) * Vector3.right + Mathf.Sin(num7) * Vector3.forward;
				array[num + i] = vector + vector3;
				array[numSegments + i] = vector2 + vector3;
				array2[num + i] = new Vector3(0f, -1f, 0f);
				array2[numSegments + i] = new Vector3(0f, 1f, 0f);
				array3[num5++] = num3;
				array3[num5++] = num + i;
				array3[num5++] = num + (i + 1) % numSegments;
				array3[num5++] = num4;
				array3[num5++] = numSegments + (i + 1) % numSegments;
				array3[num5++] = numSegments + i;
				num7 += num6;
				array[num2 + i * 2] = vector + vector3;
				array[num2 + i * 2 + 1] = vector2 + vector3;
				array2[num2 + i * 2] = vector3;
				array2[num2 + i * 2 + 1] = vector3;
				array3[num5++] = num2 + i * 2;
				array3[num5++] = num2 + (i * 2 + 3) % (numSegments * 2);
				array3[num5++] = num2 + (i * 2 + 2) % (numSegments * 2);
				array3[num5++] = num2 + i * 2;
				array3[num5++] = num2 + (i * 2 + 1) % (numSegments * 2);
				array3[num5++] = num2 + (i * 2 + 3) % (numSegments * 2);
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_cylinderSmoothShadedMeshPool.ContainsKey(numSegments))
			{
				s_cylinderSmoothShadedMeshPool.Remove(numSegments);
			}
			s_cylinderSmoothShadedMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh SphereWireframe(int latSegments, int longSegments)
	{
		if (latSegments <= 0 || longSegments <= 1)
		{
			return null;
		}
		if (s_sphereWireframeMeshPool == null)
		{
			s_sphereWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegments << 16) ^ longSegments;
		if (!s_sphereWireframeMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegments * (latSegments - 1) + 2];
			int[] array2 = new int[longSegments * (latSegments * 2 - 1) * 2];
			Vector3 vector = new Vector3(0f, 1f, 0f);
			Vector3 vector2 = new Vector3(0f, -1f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			float[] array3 = new float[latSegments];
			float[] array4 = new float[latSegments];
			float num3 = MathF.PI / (float)latSegments;
			float num4 = 0f;
			for (int i = 0; i < latSegments; i++)
			{
				num4 += num3;
				array3[i] = Mathf.Sin(num4);
				array4[i] = Mathf.Cos(num4);
			}
			float[] array5 = new float[longSegments];
			float[] array6 = new float[longSegments];
			float num5 = MathF.PI * 2f / (float)longSegments;
			float num6 = 0f;
			for (int j = 0; j < longSegments; j++)
			{
				num6 += num5;
				array5[j] = Mathf.Sin(num6);
				array6[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int num8 = 0;
			for (int k = 0; k < longSegments; k++)
			{
				float num9 = array5[k];
				float num10 = array6[k];
				for (int l = 0; l < latSegments - 1; l++)
				{
					float num11 = array3[l];
					float y = array4[l];
					array[num7] = new Vector3(num10 * num11, y, num9 * num11);
					if (l == 0)
					{
						array2[num8++] = num;
						array2[num8++] = num7;
					}
					else
					{
						array2[num8++] = num7 - 1;
						array2[num8++] = num7;
					}
					array2[num8++] = num7;
					array2[num8++] = (num7 + latSegments - 1) % (longSegments * (latSegments - 1));
					if (l == latSegments - 2)
					{
						array2[num8++] = num7;
						array2[num8++] = num2;
					}
					num7++;
				}
			}
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.Lines, 0);
			if (s_sphereWireframeMeshPool.ContainsKey(key))
			{
				s_sphereWireframeMeshPool.Remove(key);
			}
			s_sphereWireframeMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh SphereSolidColor(int latSegments, int longSegments)
	{
		if (latSegments <= 0 || longSegments <= 1)
		{
			return null;
		}
		if (s_sphereSolidColorMeshPool == null)
		{
			s_sphereSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegments << 16) ^ longSegments;
		if (!s_sphereSolidColorMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegments * (latSegments - 1) + 2];
			int[] array2 = new int[longSegments * (latSegments - 1) * 2 * 3];
			Vector3 vector = new Vector3(0f, 1f, 0f);
			Vector3 vector2 = new Vector3(0f, -1f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			float[] array3 = new float[latSegments];
			float[] array4 = new float[latSegments];
			float num3 = MathF.PI / (float)latSegments;
			float num4 = 0f;
			for (int i = 0; i < latSegments; i++)
			{
				num4 += num3;
				array3[i] = Mathf.Sin(num4);
				array4[i] = Mathf.Cos(num4);
			}
			float[] array5 = new float[longSegments];
			float[] array6 = new float[longSegments];
			float num5 = MathF.PI * 2f / (float)longSegments;
			float num6 = 0f;
			for (int j = 0; j < longSegments; j++)
			{
				num6 += num5;
				array5[j] = Mathf.Sin(num6);
				array6[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int num8 = 0;
			for (int k = 0; k < longSegments; k++)
			{
				float num9 = array5[k];
				float num10 = array6[k];
				for (int l = 0; l < latSegments - 1; l++)
				{
					float num11 = array3[l];
					float y = array4[l];
					array[num7] = new Vector3(num10 * num11, y, num9 * num11);
					if (l == 0)
					{
						array2[num8++] = num;
						array2[num8++] = (num7 + latSegments - 1) % (longSegments * (latSegments - 1));
						array2[num8++] = num7;
					}
					if (l < latSegments - 2)
					{
						array2[num8++] = num7 + 1;
						array2[num8++] = num7;
						array2[num8++] = (num7 + latSegments - 1) % (longSegments * (latSegments - 1));
						array2[num8++] = num7 + 1;
						array2[num8++] = (num7 + latSegments - 1) % (longSegments * (latSegments - 1));
						array2[num8++] = (num7 + latSegments) % (longSegments * (latSegments - 1));
					}
					if (l == latSegments - 2)
					{
						array2[num8++] = num7;
						array2[num8++] = (num7 + latSegments - 1) % (longSegments * (latSegments - 1));
						array2[num8++] = num2;
					}
					num7++;
				}
			}
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_sphereSolidColorMeshPool.ContainsKey(key))
			{
				s_sphereSolidColorMeshPool.Remove(key);
			}
			s_sphereSolidColorMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh SphereFlatShaded(int latSegments, int longSegments)
	{
		if (latSegments <= 1 || longSegments <= 1)
		{
			return null;
		}
		if (s_sphereFlatShadedMeshPool == null)
		{
			s_sphereFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegments << 16) ^ longSegments;
		if (!s_sphereFlatShadedMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			int num = (latSegments - 2) * 4 + 6;
			int num2 = (latSegments - 2) * 2 + 2;
			Vector3[] array = new Vector3[longSegments * num];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[longSegments * num2 * 3];
			Vector3 vector = new Vector3(0f, 1f, 0f);
			Vector3 vector2 = new Vector3(0f, -1f, 0f);
			float[] array4 = new float[latSegments];
			float[] array5 = new float[latSegments];
			float num3 = MathF.PI / (float)latSegments;
			float num4 = 0f;
			for (int i = 0; i < latSegments; i++)
			{
				num4 += num3;
				array4[i] = Mathf.Sin(num4);
				array5[i] = Mathf.Cos(num4);
			}
			float[] array6 = new float[longSegments];
			float[] array7 = new float[longSegments];
			float num5 = MathF.PI * 2f / (float)longSegments;
			float num6 = 0f;
			for (int j = 0; j < longSegments; j++)
			{
				num6 += num5;
				array6[j] = Mathf.Sin(num6);
				array7[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			for (int k = 0; k < longSegments; k++)
			{
				float num10 = array6[k];
				float num11 = array7[k];
				float num12 = array6[(k + 1) % longSegments];
				float num13 = array7[(k + 1) % longSegments];
				int num14 = num7;
				array[num7++] = vector;
				array[num7++] = new Vector3(num11 * array4[0], array5[0], num10 * array4[0]);
				array[num7++] = new Vector3(num13 * array4[0], array5[0], num12 * array4[0]);
				int num15 = num7;
				array[num7++] = vector2;
				array[num7++] = new Vector3(num11 * array4[latSegments - 2], array5[latSegments - 2], num10 * array4[latSegments - 2]);
				array[num7++] = new Vector3(num13 * array4[latSegments - 2], array5[latSegments - 2], num12 * array4[latSegments - 2]);
				Vector3 normalized = Vector3.Cross(array[num14 + 2] - array[num14], array[num14 + 1] - array[num14]).normalized;
				array2[num8++] = normalized;
				array2[num8++] = normalized;
				array2[num8++] = normalized;
				Vector3 normalized2 = Vector3.Cross(array[num15 + 1] - array[num15], array[num15 + 2] - array[num15]).normalized;
				array2[num8++] = normalized2;
				array2[num8++] = normalized2;
				array2[num8++] = normalized2;
				array3[num9++] = num14;
				array3[num9++] = num14 + 2;
				array3[num9++] = num14 + 1;
				array3[num9++] = num15;
				array3[num9++] = num15 + 1;
				array3[num9++] = num15 + 2;
				for (int l = 0; l < latSegments - 2; l++)
				{
					float num16 = array4[l];
					float y = array5[l];
					float num17 = array4[l + 1];
					float y2 = array5[l + 1];
					int num18 = num7;
					array[num7++] = new Vector3(num11 * num16, y, num10 * num16);
					array[num7++] = new Vector3(num13 * num16, y, num12 * num16);
					array[num7++] = new Vector3(num13 * num17, y2, num12 * num17);
					array[num7++] = new Vector3(num11 * num17, y2, num10 * num17);
					Vector3 normalized3 = Vector3.Cross(array[num18 + 1] - array[num18], array[num18 + 2] - array[num18]).normalized;
					array2[num8++] = normalized3;
					array2[num8++] = normalized3;
					array2[num8++] = normalized3;
					array2[num8++] = normalized3;
					array3[num9++] = num18;
					array3[num9++] = num18 + 1;
					array3[num9++] = num18 + 2;
					array3[num9++] = num18;
					array3[num9++] = num18 + 2;
					array3[num9++] = num18 + 3;
				}
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_sphereFlatShadedMeshPool.ContainsKey(key))
			{
				s_sphereFlatShadedMeshPool.Remove(key);
			}
			s_sphereFlatShadedMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh SphereSmoothShaded(int latSegments, int longSegments)
	{
		if (latSegments <= 1 || longSegments <= 1)
		{
			return null;
		}
		if (s_sphereSmoothShadedMeshPool == null)
		{
			s_sphereSmoothShadedMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegments << 16) ^ longSegments;
		if (!s_sphereSmoothShadedMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			int num = latSegments - 1;
			int num2 = (latSegments - 2) * 2 + 2;
			Vector3[] array = new Vector3[longSegments * num + 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[longSegments * num2 * 3];
			Vector3 vector = new Vector3(0f, 1f, 0f);
			Vector3 vector2 = new Vector3(0f, -1f, 0f);
			int num3 = longSegments * num;
			int num4 = num3 + 1;
			array[num3] = vector;
			array[num4] = vector2;
			array2[num3] = new Vector3(0f, 1f, 0f);
			array2[num4] = new Vector3(0f, -1f, 0f);
			float[] array4 = new float[latSegments];
			float[] array5 = new float[latSegments];
			float num5 = MathF.PI / (float)latSegments;
			float num6 = 0f;
			for (int i = 0; i < latSegments; i++)
			{
				num6 += num5;
				array4[i] = Mathf.Sin(num6);
				array5[i] = Mathf.Cos(num6);
			}
			float[] array6 = new float[longSegments];
			float[] array7 = new float[longSegments];
			float num7 = MathF.PI * 2f / (float)longSegments;
			float num8 = 0f;
			for (int j = 0; j < longSegments; j++)
			{
				num8 += num7;
				array6[j] = Mathf.Sin(num8);
				array7[j] = Mathf.Cos(num8);
			}
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			for (int k = 0; k < longSegments; k++)
			{
				float num12 = array6[k];
				float num13 = array7[k];
				for (int l = 0; l < latSegments - 1; l++)
				{
					float num14 = array4[l];
					float y = array5[l];
					Vector3 vector3 = new Vector3(num13 * num14, y, num12 * num14);
					array[num9++] = vector3;
					array2[num10++] = vector3;
					int num15 = num9 - 1;
					int num16 = num15 + 1;
					int num17 = (num15 + num) % (longSegments * num);
					int num18 = (num15 + num + 1) % (longSegments * num);
					if (latSegments == 2)
					{
						array3[num11++] = num3;
						array3[num11++] = num17;
						array3[num11++] = num15;
						array3[num11++] = num4;
						array3[num11++] = num16;
						array3[num11++] = num18;
					}
					else if (l < latSegments - 2)
					{
						if (l == 0)
						{
							array3[num11++] = num3;
							array3[num11++] = num17;
							array3[num11++] = num15;
						}
						else if (l == latSegments - 3)
						{
							array3[num11++] = num4;
							array3[num11++] = num16;
							array3[num11++] = num18;
						}
						array3[num11++] = num15;
						array3[num11++] = num18;
						array3[num11++] = num16;
						array3[num11++] = num15;
						array3[num11++] = num17;
						array3[num11++] = num18;
					}
				}
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_sphereSmoothShadedMeshPool.ContainsKey(key))
			{
				s_sphereSmoothShadedMeshPool.Remove(key);
			}
			s_sphereSmoothShadedMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh CapsuleWireframe(int latSegmentsPerCap, int longSegmentsPerCap, bool caps = true, bool topCapOnly = false, bool sides = true)
	{
		if (latSegmentsPerCap <= 0 || longSegmentsPerCap <= 1)
		{
			return null;
		}
		if (!caps && !sides)
		{
			return null;
		}
		if (s_capsuleWireframeMeshPool == null)
		{
			s_capsuleWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegmentsPerCap << 12) ^ longSegmentsPerCap ^ (caps ? 268435456 : 0) ^ (topCapOnly ? 536870912 : 0) ^ (sides ? 1073741824 : 0);
		if (!s_capsuleWireframeMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegmentsPerCap * latSegmentsPerCap * 2 + 2];
			int[] array2 = new int[longSegmentsPerCap * (latSegmentsPerCap * 4 + 1) * 2];
			Vector3 vector = new Vector3(0f, 1.5f, 0f);
			Vector3 vector2 = new Vector3(0f, -1.5f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			float[] array3 = new float[latSegmentsPerCap];
			float[] array4 = new float[latSegmentsPerCap];
			float num3 = MathF.PI / 2f / (float)latSegmentsPerCap;
			float num4 = 0f;
			for (int i = 0; i < latSegmentsPerCap; i++)
			{
				num4 += num3;
				array3[i] = Mathf.Sin(num4);
				array4[i] = Mathf.Cos(num4);
			}
			float[] array5 = new float[longSegmentsPerCap];
			float[] array6 = new float[longSegmentsPerCap];
			float num5 = MathF.PI * 2f / (float)longSegmentsPerCap;
			float num6 = 0f;
			for (int j = 0; j < longSegmentsPerCap; j++)
			{
				num6 += num5;
				array5[j] = Mathf.Sin(num6);
				array6[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int newSize = 0;
			for (int k = 0; k < longSegmentsPerCap; k++)
			{
				float num8 = array5[k];
				float num9 = array6[k];
				for (int l = 0; l < latSegmentsPerCap; l++)
				{
					float num10 = array3[l];
					float num11 = array4[l];
					array[num7] = new Vector3(num9 * num10, num11 + 0.5f, num8 * num10);
					array[num7 + 1] = new Vector3(num9 * num10, 0f - num11 - 0.5f, num8 * num10);
					if (caps)
					{
						if (l == 0)
						{
							array2[newSize++] = num;
							array2[newSize++] = num7;
							if (!topCapOnly)
							{
								array2[newSize++] = num2;
								array2[newSize++] = num7 + 1;
							}
						}
						else
						{
							array2[newSize++] = num7 - 2;
							array2[newSize++] = num7;
							if (!topCapOnly)
							{
								array2[newSize++] = num7 - 1;
								array2[newSize++] = num7 + 1;
							}
						}
					}
					if (caps || l == latSegmentsPerCap - 1)
					{
						array2[newSize++] = num7;
						array2[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						if (!topCapOnly)
						{
							array2[newSize++] = num7 + 1;
							array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						}
					}
					if (sides && l == latSegmentsPerCap - 1)
					{
						array2[newSize++] = num7;
						array2[newSize++] = num7 + 1;
					}
					num7 += 2;
				}
			}
			Array.Resize(ref array2, newSize);
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.Lines, 0);
			if (s_capsuleWireframeMeshPool.ContainsKey(key))
			{
				s_capsuleWireframeMeshPool.Remove(key);
			}
			s_capsuleWireframeMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh CapsuleSolidColor(int latSegmentsPerCap, int longSegmentsPerCap, bool caps = true, bool topCapOnly = false, bool sides = true)
	{
		if (latSegmentsPerCap <= 0 || longSegmentsPerCap <= 1)
		{
			return null;
		}
		if (!caps && !sides)
		{
			return null;
		}
		if (s_capsuleSolidColorMeshPool == null)
		{
			s_capsuleSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegmentsPerCap << 12) ^ longSegmentsPerCap ^ (caps ? 268435456 : 0) ^ (topCapOnly ? 536870912 : 0) ^ (sides ? 1073741824 : 0);
		if (!s_capsuleSolidColorMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegmentsPerCap * latSegmentsPerCap * 2 + 2];
			int[] array2 = new int[longSegmentsPerCap * (latSegmentsPerCap * 4) * 3];
			Vector3 vector = new Vector3(0f, 1.5f, 0f);
			Vector3 vector2 = new Vector3(0f, -1.5f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			float[] array3 = new float[latSegmentsPerCap];
			float[] array4 = new float[latSegmentsPerCap];
			float num3 = MathF.PI / 2f / (float)latSegmentsPerCap;
			float num4 = 0f;
			for (int i = 0; i < latSegmentsPerCap; i++)
			{
				num4 += num3;
				array3[i] = Mathf.Sin(num4);
				array4[i] = Mathf.Cos(num4);
			}
			float[] array5 = new float[longSegmentsPerCap];
			float[] array6 = new float[longSegmentsPerCap];
			float num5 = MathF.PI * 2f / (float)longSegmentsPerCap;
			float num6 = 0f;
			for (int j = 0; j < longSegmentsPerCap; j++)
			{
				num6 += num5;
				array5[j] = Mathf.Sin(num6);
				array6[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int newSize = 0;
			for (int k = 0; k < longSegmentsPerCap; k++)
			{
				float num8 = array5[k];
				float num9 = array6[k];
				for (int l = 0; l < latSegmentsPerCap; l++)
				{
					float num10 = array3[l];
					float num11 = array4[l];
					array[num7] = new Vector3(num9 * num10, num11 + 0.5f, num8 * num10);
					array[num7 + 1] = new Vector3(num9 * num10, 0f - num11 - 0.5f, num8 * num10);
					if (l == 0)
					{
						if (caps)
						{
							array2[newSize++] = num;
							array2[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array2[newSize++] = num7;
							if (!topCapOnly)
							{
								array2[newSize++] = num2;
								array2[newSize++] = num7 + 1;
								array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							}
						}
					}
					else
					{
						if (caps)
						{
							array2[newSize++] = num7 - 2;
							array2[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array2[newSize++] = num7;
							array2[newSize++] = num7 - 2;
							array2[newSize++] = (num7 - 2 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array2[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							if (!topCapOnly)
							{
								array2[newSize++] = num7 - 1;
								array2[newSize++] = num7 + 1;
								array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
								array2[newSize++] = num7 - 1;
								array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
								array2[newSize++] = (num7 - 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							}
						}
						if (sides && l == latSegmentsPerCap - 1)
						{
							array2[newSize++] = num7;
							array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array2[newSize++] = num7 + 1;
							array2[newSize++] = num7;
							array2[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array2[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						}
					}
					num7 += 2;
				}
			}
			Array.Resize(ref array2, newSize);
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_capsuleSolidColorMeshPool.ContainsKey(key))
			{
				s_capsuleSolidColorMeshPool.Remove(key);
			}
			s_capsuleSolidColorMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh CapsuleFlatShaded(int latSegmentsPerCap, int longSegmentsPerCap, bool caps = true, bool topCapOnly = false, bool sides = true)
	{
		if (latSegmentsPerCap <= 0 || longSegmentsPerCap <= 1)
		{
			return null;
		}
		if (!caps && !sides)
		{
			return null;
		}
		if (s_capsuleFlatShadedMeshPool == null)
		{
			s_capsuleFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegmentsPerCap << 12) ^ longSegmentsPerCap ^ (caps ? 268435456 : 0) ^ (topCapOnly ? 536870912 : 0) ^ (sides ? 1073741824 : 0);
		if (!s_capsuleFlatShadedMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegmentsPerCap * (latSegmentsPerCap - 1) * 8 + longSegmentsPerCap * 10];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[longSegmentsPerCap * (latSegmentsPerCap * 4) * 3];
			Vector3 vector = new Vector3(0f, 1.5f, 0f);
			Vector3 vector2 = new Vector3(0f, -1.5f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			array2[num] = new Vector3(0f, 1f, 0f);
			array2[num2] = new Vector3(0f, -1f, 0f);
			float[] array4 = new float[latSegmentsPerCap];
			float[] array5 = new float[latSegmentsPerCap];
			float num3 = MathF.PI / 2f / (float)latSegmentsPerCap;
			float num4 = 0f;
			for (int i = 0; i < latSegmentsPerCap; i++)
			{
				num4 += num3;
				array4[i] = Mathf.Sin(num4);
				array5[i] = Mathf.Cos(num4);
			}
			float[] array6 = new float[longSegmentsPerCap];
			float[] array7 = new float[longSegmentsPerCap];
			float num5 = MathF.PI * 2f / (float)longSegmentsPerCap;
			float num6 = 0f;
			for (int j = 0; j < longSegmentsPerCap; j++)
			{
				num6 += num5;
				array6[j] = Mathf.Sin(num6);
				array7[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int num8 = 0;
			int newSize = 0;
			for (int k = 0; k < longSegmentsPerCap; k++)
			{
				float num9 = array6[k];
				float num10 = array7[k];
				float num11 = array6[(k + 1) % longSegmentsPerCap];
				float num12 = array7[(k + 1) % longSegmentsPerCap];
				for (int l = 0; l < latSegmentsPerCap; l++)
				{
					float num13 = array4[l];
					float num14 = array5[l];
					if (caps && l < latSegmentsPerCap - 1)
					{
						if (l == 0)
						{
							int num15 = num7;
							array[num7++] = vector;
							array[num7++] = new Vector3(num10 * num13, num14 + 0.5f, num9 * num13);
							array[num7++] = new Vector3(num12 * num13, num14 + 0.5f, num11 * num13);
							Vector3 vector3 = Vector3.Cross(array[num15 + 2] - array[num15], array[num15 + 1] - array[num15]);
							array2[num8++] = vector3;
							array2[num8++] = vector3;
							array2[num8++] = vector3;
							array3[newSize++] = num15;
							array3[newSize++] = num15 + 2;
							array3[newSize++] = num15 + 1;
							if (!topCapOnly)
							{
								int num16 = num7;
								array[num7++] = vector2;
								array[num7++] = new Vector3(num10 * num13, 0f - num14 - 0.5f, num9 * num13);
								array[num7++] = new Vector3(num12 * num13, 0f - num14 - 0.5f, num11 * num13);
								Vector3 normalized = Vector3.Cross(array[num16 + 1] - array[num16], array[num16 + 2] - array[num16]).normalized;
								array2[num8++] = normalized;
								array2[num8++] = normalized;
								array2[num8++] = normalized;
								array3[newSize++] = num16;
								array3[newSize++] = num16 + 1;
								array3[newSize++] = num16 + 2;
							}
						}
						float num17 = array4[l + 1];
						float num18 = array5[l + 1];
						if (caps)
						{
							int num19 = num7;
							array[num7++] = new Vector3(num10 * num13, num14 + 0.5f, num9 * num13);
							array[num7++] = new Vector3(num10 * num17, num18 + 0.5f, num9 * num17);
							array[num7++] = new Vector3(num12 * num17, num18 + 0.5f, num11 * num17);
							array[num7++] = new Vector3(num12 * num13, num14 + 0.5f, num11 * num13);
							Vector3 vector4 = Vector3.Cross(array[num19 + 3] - array[num19], array[num19 + 1] - array[num19]);
							array2[num8++] = vector4;
							array2[num8++] = vector4;
							array2[num8++] = vector4;
							array2[num8++] = vector4;
							array3[newSize++] = num19;
							array3[newSize++] = num19 + 2;
							array3[newSize++] = num19 + 1;
							array3[newSize++] = num19;
							array3[newSize++] = num19 + 3;
							array3[newSize++] = num19 + 2;
							if (!topCapOnly)
							{
								int num20 = num7;
								array[num7++] = new Vector3(num10 * num13, 0f - num14 - 0.5f, num9 * num13);
								array[num7++] = new Vector3(num10 * num17, 0f - num18 - 0.5f, num9 * num17);
								array[num7++] = new Vector3(num12 * num17, 0f - num18 - 0.5f, num11 * num17);
								array[num7++] = new Vector3(num12 * num13, 0f - num14 - 0.5f, num11 * num13);
								Vector3 vector5 = Vector3.Cross(array[num20 + 1] - array[num20], array[num20 + 3] - array[num20]);
								array2[num8++] = vector5;
								array2[num8++] = vector5;
								array2[num8++] = vector5;
								array2[num8++] = vector5;
								array3[newSize++] = num20;
								array3[newSize++] = num20 + 1;
								array3[newSize++] = num20 + 2;
								array3[newSize++] = num20;
								array3[newSize++] = num20 + 2;
								array3[newSize++] = num20 + 3;
							}
						}
					}
					else if (sides && l == latSegmentsPerCap - 1)
					{
						int num21 = num7;
						array[num7++] = new Vector3(num10 * num13, num14 + 0.5f, num9 * num13);
						array[num7++] = new Vector3(num10 * num13, 0f - num14 - 0.5f, num9 * num13);
						array[num7++] = new Vector3(num12 * num13, 0f - num14 - 0.5f, num11 * num13);
						array[num7++] = new Vector3(num12 * num13, num14 + 0.5f, num11 * num13);
						Vector3 normalized2 = Vector3.Cross(array[num21 + 3] - array[num21], array[num21 + 1] - array[num21]).normalized;
						array2[num8++] = normalized2;
						array2[num8++] = normalized2;
						array2[num8++] = normalized2;
						array2[num8++] = normalized2;
						array3[newSize++] = num21;
						array3[newSize++] = num21 + 2;
						array3[newSize++] = num21 + 1;
						array3[newSize++] = num21;
						array3[newSize++] = num21 + 3;
						array3[newSize++] = num21 + 2;
					}
				}
			}
			Array.Resize(ref array3, newSize);
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_capsuleFlatShadedMeshPool.ContainsKey(key))
			{
				s_capsuleFlatShadedMeshPool.Remove(key);
			}
			s_capsuleFlatShadedMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh CapsuleSmoothShaded(int latSegmentsPerCap, int longSegmentsPerCap, bool caps = true, bool topCapOnly = false, bool sides = true)
	{
		if (latSegmentsPerCap <= 0 || longSegmentsPerCap <= 1)
		{
			return null;
		}
		if (!caps && !sides)
		{
			return null;
		}
		if (s_capsuleSmoothShadedMeshPool == null)
		{
			s_capsuleSmoothShadedMeshPool = new Dictionary<int, Mesh>();
		}
		int key = (latSegmentsPerCap << 12) ^ longSegmentsPerCap ^ (caps ? 268435456 : 0) ^ (topCapOnly ? 536870912 : 0) ^ (sides ? 1073741824 : 0);
		if (!s_capsuleSmoothShadedMeshPool.TryGetValue(key, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[longSegmentsPerCap * latSegmentsPerCap * 2 + 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[longSegmentsPerCap * (latSegmentsPerCap * 4) * 3];
			Vector3 vector = new Vector3(0f, 1.5f, 0f);
			Vector3 vector2 = new Vector3(0f, -1.5f, 0f);
			int num = array.Length - 2;
			int num2 = array.Length - 1;
			array[num] = vector;
			array[num2] = vector2;
			array2[num] = new Vector3(0f, 1f, 0f);
			array2[num2] = new Vector3(0f, -1f, 0f);
			float[] array4 = new float[latSegmentsPerCap];
			float[] array5 = new float[latSegmentsPerCap];
			float num3 = MathF.PI / 2f / (float)latSegmentsPerCap;
			float num4 = 0f;
			for (int i = 0; i < latSegmentsPerCap; i++)
			{
				num4 += num3;
				array4[i] = Mathf.Sin(num4);
				array5[i] = Mathf.Cos(num4);
			}
			float[] array6 = new float[longSegmentsPerCap];
			float[] array7 = new float[longSegmentsPerCap];
			float num5 = MathF.PI * 2f / (float)longSegmentsPerCap;
			float num6 = 0f;
			for (int j = 0; j < longSegmentsPerCap; j++)
			{
				num6 += num5;
				array6[j] = Mathf.Sin(num6);
				array7[j] = Mathf.Cos(num6);
			}
			int num7 = 0;
			int num8 = 0;
			int newSize = 0;
			for (int k = 0; k < longSegmentsPerCap; k++)
			{
				float num9 = array6[k];
				float num10 = array7[k];
				for (int l = 0; l < latSegmentsPerCap; l++)
				{
					float num11 = array4[l];
					float num12 = array5[l];
					array[num7] = new Vector3(num10 * num11, num12 + 0.5f, num9 * num11);
					array[num7 + 1] = new Vector3(num10 * num11, 0f - num12 - 0.5f, num9 * num11);
					array2[num8] = new Vector3(num10 * num11, num12, num9 * num11);
					array2[num8 + 1] = new Vector3(num10 * num11, 0f - num12, num9 * num11);
					if (caps && l == 0)
					{
						array3[newSize++] = num;
						array3[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						array3[newSize++] = num7;
						if (!topCapOnly)
						{
							array3[newSize++] = num2;
							array3[newSize++] = num7 + 1;
							array3[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						}
					}
					else
					{
						if (caps)
						{
							array3[newSize++] = num7 - 2;
							array3[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array3[newSize++] = num7;
							array3[newSize++] = num7 - 2;
							array3[newSize++] = (num7 - 2 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array3[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							if (!topCapOnly)
							{
								array3[newSize++] = num7 - 1;
								array3[newSize++] = num7 + 1;
								array3[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
								array3[newSize++] = num7 - 1;
								array3[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
								array3[newSize++] = (num7 - 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							}
						}
						if (sides && l == latSegmentsPerCap - 1)
						{
							array3[newSize++] = num7;
							array3[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array3[newSize++] = num7 + 1;
							array3[newSize++] = num7;
							array3[newSize++] = (num7 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
							array3[newSize++] = (num7 + 1 + latSegmentsPerCap * 2) % (longSegmentsPerCap * latSegmentsPerCap * 2);
						}
					}
					num7 += 2;
					num8 += 2;
				}
			}
			Array.Resize(ref array3, newSize);
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_capsuleSmoothShadedMeshPool.ContainsKey(key))
			{
				s_capsuleSmoothShadedMeshPool.Remove(key);
			}
			s_capsuleSmoothShadedMeshPool.Add(key, value);
		}
		return value;
	}

	public static Mesh Capsule2DWireframe(int capSegments)
	{
		if (capSegments <= 0)
		{
			return null;
		}
		if (s_capsule2dWireframeMeshPool == null)
		{
			s_capsule2dWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_capsule2dWireframeMeshPool.TryGetValue(capSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[(capSegments + 1) * 2];
			int[] array2 = new int[(capSegments + 1) * 4];
			int num = 0;
			int num2 = 0;
			float num3 = MathF.PI / (float)capSegments;
			float num4 = 0f;
			for (int i = 0; i < capSegments; i++)
			{
				array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) + 0.5f, 0f);
				num4 += num3;
			}
			array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) + 0.5f, 0f);
			for (int j = 0; j < capSegments; j++)
			{
				array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) - 0.5f, 0f);
				num4 += num3;
			}
			array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) - 0.5f, 0f);
			for (int k = 0; k < array.Length - 1; k++)
			{
				array2[num2++] = k;
				array2[num2++] = (k + 1) % array.Length;
			}
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.LineStrip, 0);
			if (s_capsule2dWireframeMeshPool.ContainsKey(capSegments))
			{
				s_capsule2dWireframeMeshPool.Remove(capSegments);
			}
			s_capsule2dWireframeMeshPool.Add(capSegments, value);
		}
		return value;
	}

	public static Mesh Capsule2DSolidColor(int capSegments)
	{
		if (capSegments <= 0)
		{
			return null;
		}
		if (s_capsule2dSolidColorMeshPool == null)
		{
			s_capsule2dSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_capsule2dSolidColorMeshPool.TryGetValue(capSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[(capSegments + 1) * 2];
			int[] array2 = new int[(capSegments + 1) * 12];
			int num = 0;
			int num2 = 0;
			float num3 = MathF.PI / (float)capSegments;
			float num4 = 0f;
			for (int i = 0; i < capSegments; i++)
			{
				array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) + 0.5f, 0f);
				num4 += num3;
			}
			array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) + 0.5f, 0f);
			for (int j = 0; j < capSegments; j++)
			{
				array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) - 0.5f, 0f);
				num4 += num3;
			}
			array[num++] = new Vector3(Mathf.Cos(num4), Mathf.Sin(num4) - 0.5f, 0f);
			for (int k = 1; k < array.Length; k++)
			{
				array2[num2++] = 0;
				array2[num2++] = (k + 1) % array.Length;
				array2[num2++] = k;
				array2[num2++] = 0;
				array2[num2++] = k;
				array2[num2++] = (k + 1) % array.Length;
			}
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_capsule2dSolidColorMeshPool.ContainsKey(capSegments))
			{
				s_capsule2dSolidColorMeshPool.Remove(capSegments);
			}
			s_capsule2dSolidColorMeshPool.Add(capSegments, value);
		}
		return value;
	}

	public static Mesh Capsule2DFlatShaded(int capSegments)
	{
		if (capSegments <= 0)
		{
			return null;
		}
		if (s_capsule2dFlatShadedMeshPool == null)
		{
			s_capsule2dFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_capsule2dFlatShadedMeshPool.TryGetValue(capSegments, out var value) || value == null)
		{
			value = new Mesh();
			int num = (capSegments + 1) * 2;
			Vector3[] array = new Vector3[num * 2];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[num * 6];
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			float num5 = MathF.PI / (float)capSegments;
			float num6 = 0f;
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < capSegments; j++)
				{
					array[num2++] = new Vector3(Mathf.Cos(num6), Mathf.Sin(num6) + 0.5f, 0f);
					num6 += num5;
				}
				array[num2++] = new Vector3(Mathf.Cos(num6), Mathf.Sin(num6) + 0.5f, 0f);
				for (int k = 0; k < capSegments; k++)
				{
					array[num2++] = new Vector3(Mathf.Cos(num6), Mathf.Sin(num6) - 0.5f, 0f);
					num6 += num5;
				}
				array[num2++] = new Vector3(Mathf.Cos(num6), Mathf.Sin(num6) - 0.5f, 0f);
				Vector3 vector = new Vector3(0f, 0f, (i == 0) ? (-1f) : 1f);
				for (int l = 0; l < num; l++)
				{
					array2[num3++] = vector;
				}
			}
			for (int m = 1; m < num; m++)
			{
				array3[num4++] = 0;
				array3[num4++] = (m + 1) % num;
				array3[num4++] = m;
				array3[num4++] = num;
				array3[num4++] = num + m;
				array3[num4++] = num + (m + 1) % num;
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_capsule2dFlatShadedMeshPool.ContainsKey(capSegments))
			{
				s_capsule2dFlatShadedMeshPool.Remove(capSegments);
			}
			s_capsule2dFlatShadedMeshPool.Add(capSegments, value);
		}
		return value;
	}

	public static Mesh ConeWireframe(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_coneWireframeMeshPool == null)
		{
			s_coneWireframeMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_coneWireframeMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments + 1];
			int[] array2 = new int[numSegments * 4];
			array[numSegments] = new Vector3(0f, 1f, 0f);
			int num = 0;
			float num2 = MathF.PI * 2f / (float)numSegments;
			float num3 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array[i] = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				array2[num++] = i;
				array2[num++] = (i + 1) % numSegments;
				array2[num++] = i;
				array2[num++] = numSegments;
				num3 += num2;
			}
			value.vertices = array;
			value.normals = array;
			value.SetIndices(array2, MeshTopology.Lines, 0);
			if (s_coneWireframeMeshPool.ContainsKey(numSegments))
			{
				s_coneWireframeMeshPool.Remove(numSegments);
			}
			s_coneWireframeMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh ConeSolidColor(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_coneSolidColorMeshPool == null)
		{
			s_coneSolidColorMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_coneSolidColorMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments + 1];
			int[] array2 = new int[numSegments * 3 + (numSegments - 2) * 3];
			array[numSegments] = new Vector3(0f, 1f, 0f);
			int num = 0;
			float num2 = MathF.PI * 2f / (float)numSegments;
			float num3 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array[i] = Mathf.Cos(num3) * Vector3.right + Mathf.Sin(num3) * Vector3.forward;
				array2[num++] = numSegments;
				array2[num++] = (i + 1) % numSegments;
				array2[num++] = i;
				if (i >= 2)
				{
					array2[num++] = 0;
					array2[num++] = i - 1;
					array2[num++] = i;
				}
				num3 += num2;
			}
			value.vertices = array;
			value.SetIndices(array2, MeshTopology.Triangles, 0);
			if (s_coneSolidColorMeshPool.ContainsKey(numSegments))
			{
				s_coneSolidColorMeshPool.Remove(numSegments);
			}
			s_coneSolidColorMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh ConeFlatShaded(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_coneFlatShadedMeshPool == null)
		{
			s_coneFlatShadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_coneFlatShadedMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 3 + numSegments];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[numSegments * 3 + (numSegments - 2) * 3];
			Vector3 vector = new Vector3(0f, 1f, 0f);
			Vector3[] array4 = new Vector3[numSegments];
			float num = MathF.PI * 2f / (float)numSegments;
			float num2 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				array4[i] = Mathf.Cos(num2) * Vector3.right + Mathf.Sin(num2) * Vector3.forward;
				num2 += num;
			}
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			for (int j = 0; j < numSegments; j++)
			{
				int num6 = num3;
				array[num3++] = vector;
				array[num3++] = array4[j];
				array[num3++] = array4[(j + 1) % numSegments];
				Vector3 normalized = Vector3.Cross(array[num6 + 2] - array[num6], array[num6 + 1] - array[num6]).normalized;
				array2[num5++] = normalized;
				array2[num5++] = normalized;
				array2[num5++] = normalized;
				array3[num4++] = num6;
				array3[num4++] = num6 + 2;
				array3[num4++] = num6 + 1;
			}
			int num7 = num3;
			for (int k = 0; k < numSegments; k++)
			{
				array[num3++] = array4[k];
				array2[num5++] = new Vector3(0f, -1f, 0f);
				if (k >= 2)
				{
					array3[num4++] = num7;
					array3[num4++] = num7 + k - 1;
					array3[num4++] = num7 + k;
				}
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_coneFlatShadedMeshPool.ContainsKey(numSegments))
			{
				s_coneFlatShadedMeshPool.Remove(numSegments);
			}
			s_coneFlatShadedMeshPool.Add(numSegments, value);
		}
		return value;
	}

	public static Mesh ConeSmoothShaded(int numSegments)
	{
		if (numSegments <= 1)
		{
			return null;
		}
		if (s_coneSmoothhadedMeshPool == null)
		{
			s_coneSmoothhadedMeshPool = new Dictionary<int, Mesh>();
		}
		if (!s_coneSmoothhadedMeshPool.TryGetValue(numSegments, out var value) || value == null)
		{
			value = new Mesh();
			Vector3[] array = new Vector3[numSegments * 2 + 1];
			Vector3[] array2 = new Vector3[array.Length];
			int[] array3 = new int[numSegments * 3 + (numSegments - 2) * 3];
			int num = array.Length - 1;
			array[num] = new Vector3(0f, 1f, 0f);
			array2[num] = new Vector3(0f, 0f, 0f);
			float num2 = Mathf.Sqrt(0.5f);
			int num3 = 0;
			float num4 = MathF.PI * 2f / (float)numSegments;
			float num5 = 0f;
			for (int i = 0; i < numSegments; i++)
			{
				float num6 = Mathf.Cos(num5);
				float num7 = Mathf.Sin(num5);
				array[numSegments + i] = (array[i] = num6 * Vector3.right + num7 * Vector3.forward);
				array2[i] = new Vector3(num6 * num2, num2, num7 * num2);
				array2[numSegments + i] = new Vector3(0f, -1f, 0f);
				array3[num3++] = num;
				array3[num3++] = (i + 1) % numSegments;
				array3[num3++] = i;
				if (i >= 2)
				{
					array3[num3++] = numSegments;
					array3[num3++] = numSegments + i - 1;
					array3[num3++] = numSegments + i;
				}
				num5 += num4;
			}
			value.vertices = array;
			value.normals = array2;
			value.SetIndices(array3, MeshTopology.Triangles, 0);
			if (s_coneSmoothhadedMeshPool.ContainsKey(numSegments))
			{
				s_coneSmoothhadedMeshPool.Remove(numSegments);
			}
			s_coneSmoothhadedMeshPool.Add(numSegments, value);
		}
		return value;
	}
}
