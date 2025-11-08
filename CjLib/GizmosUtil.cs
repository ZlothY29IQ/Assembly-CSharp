using UnityEngine;

namespace CjLib;

public class GizmosUtil
{
	public enum Style
	{
		Wireframe,
		FlatShaded,
		SmoothShaded
	}

	public static void DrawLine(Vector3 v0, Vector3 v1, Color color)
	{
		Gizmos.color = color;
		Gizmos.DrawLine(v0, v1);
	}

	public static void DrawLines(Vector3[] aVert, Color color)
	{
		Gizmos.color = color;
		for (int i = 0; i < aVert.Length; i += 2)
		{
			Gizmos.DrawLine(aVert[i], aVert[i + 1]);
		}
	}

	public static void DrawLineStrip(Vector3[] aVert, Color color)
	{
		Gizmos.color = color;
		for (int i = 0; i < aVert.Length; i++)
		{
			Gizmos.DrawLine(aVert[i], aVert[i + 1]);
		}
	}

	public static void DrawBox(Vector3 center, Quaternion rotation, Vector3 dimensions, Color color, Style style = Style.FlatShaded)
	{
		if (dimensions.x < MathUtil.Epsilon || dimensions.y < MathUtil.Epsilon || dimensions.z < MathUtil.Epsilon)
		{
			return;
		}
		Mesh mesh = null;
		switch (style)
		{
		case Style.Wireframe:
			mesh = PrimitiveMeshFactory.BoxWireframe();
			break;
		case Style.FlatShaded:
		case Style.SmoothShaded:
			mesh = PrimitiveMeshFactory.BoxFlatShaded();
			break;
		}
		if (!(mesh == null))
		{
			Gizmos.color = color;
			if (style == Style.Wireframe)
			{
				Gizmos.DrawWireMesh(mesh, center, rotation, dimensions);
			}
			else
			{
				Gizmos.DrawMesh(mesh, center, rotation, dimensions);
			}
		}
	}

	public static void DrawCylinder(Vector3 center, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style = Style.SmoothShaded)
	{
		if (height < MathUtil.Epsilon || radius < MathUtil.Epsilon)
		{
			return;
		}
		Mesh mesh = null;
		switch (style)
		{
		case Style.Wireframe:
			mesh = PrimitiveMeshFactory.CylinderWireframe(numSegments);
			break;
		case Style.FlatShaded:
			mesh = PrimitiveMeshFactory.CylinderFlatShaded(numSegments);
			break;
		case Style.SmoothShaded:
			mesh = PrimitiveMeshFactory.CylinderSmoothShaded(numSegments);
			break;
		}
		if (!(mesh == null))
		{
			Gizmos.color = color;
			if (style == Style.Wireframe)
			{
				Gizmos.DrawWireMesh(mesh, center, rotation, new Vector3(radius, height, radius));
			}
			else
			{
				Gizmos.DrawMesh(mesh, center, rotation, new Vector3(radius, height, radius));
			}
		}
	}

	public static void DrawCylinder(Vector3 point0, Vector3 point1, float radius, int numSegments, Color color, Style style = Style.SmoothShaded)
	{
		Vector3 vector = point1 - point0;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Vector3 center = 0.5f * (point0 + point1);
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Vector3.Dot(vector.normalized, Vector3.up) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCylinder(center, rotation, magnitude, radius, numSegments, color, style);
		}
	}

	public static void DrawSphere(Vector3 center, Quaternion rotation, float radius, int latSegments, int longSegments, Color color, Style style = Style.SmoothShaded)
	{
		if (radius < MathUtil.Epsilon)
		{
			return;
		}
		Mesh mesh = null;
		switch (style)
		{
		case Style.Wireframe:
			mesh = PrimitiveMeshFactory.SphereWireframe(latSegments, longSegments);
			break;
		case Style.FlatShaded:
			mesh = PrimitiveMeshFactory.SphereFlatShaded(latSegments, longSegments);
			break;
		case Style.SmoothShaded:
			mesh = PrimitiveMeshFactory.SphereSmoothShaded(latSegments, longSegments);
			break;
		}
		if (!(mesh == null))
		{
			Gizmos.color = color;
			if (style == Style.Wireframe)
			{
				Gizmos.DrawWireMesh(mesh, center, rotation, new Vector3(radius, radius, radius));
			}
			else
			{
				Gizmos.DrawMesh(mesh, center, rotation, new Vector3(radius, radius, radius));
			}
		}
	}

	public static void DrawSphere(Vector3 center, float radius, int latSegments, int longSegments, Color color, Style style = Style.SmoothShaded)
	{
		DrawSphere(center, Quaternion.identity, radius, latSegments, longSegments, color, style);
	}

	public static void DrawCapsule(Vector3 center, Quaternion rotation, float height, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style = Style.SmoothShaded)
	{
		if (height < MathUtil.Epsilon || radius < MathUtil.Epsilon)
		{
			return;
		}
		Mesh mesh = null;
		Mesh mesh2 = null;
		switch (style)
		{
		case Style.Wireframe:
			mesh = PrimitiveMeshFactory.CapsuleWireframe(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
			mesh2 = PrimitiveMeshFactory.CapsuleWireframe(latSegmentsPerCap, longSegmentsPerCap, caps: false);
			break;
		case Style.FlatShaded:
			mesh = PrimitiveMeshFactory.CapsuleFlatShaded(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
			mesh2 = PrimitiveMeshFactory.CapsuleFlatShaded(latSegmentsPerCap, longSegmentsPerCap, caps: false);
			break;
		case Style.SmoothShaded:
			mesh = PrimitiveMeshFactory.CapsuleSmoothShaded(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
			mesh2 = PrimitiveMeshFactory.CapsuleSmoothShaded(latSegmentsPerCap, longSegmentsPerCap, caps: false);
			break;
		}
		if (!(mesh == null) && !(mesh2 == null))
		{
			Vector3 vector = rotation * Vector3.up;
			Vector3 vector2 = 0.5f * (height - radius) * vector;
			Vector3 position = center + vector2;
			Vector3 position2 = center - vector2;
			Quaternion rotation2 = Quaternion.AngleAxis(180f, vector) * rotation;
			Gizmos.color = color;
			if (style == Style.Wireframe)
			{
				Gizmos.DrawWireMesh(mesh, position, rotation, new Vector3(radius, radius, radius));
				Gizmos.DrawWireMesh(mesh, position2, rotation2, new Vector3(0f - radius, 0f - radius, radius));
				Gizmos.DrawWireMesh(mesh2, center, rotation, new Vector3(radius, height, radius));
			}
			else
			{
				Gizmos.DrawMesh(mesh, position, rotation, new Vector3(radius, radius, radius));
				Gizmos.DrawMesh(mesh, position2, rotation2, new Vector3(0f - radius, 0f - radius, radius));
				Gizmos.DrawMesh(mesh2, center, rotation, new Vector3(radius, height, radius));
			}
		}
	}

	public static void DrawCapsule(Vector3 point0, Vector3 point1, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style = Style.SmoothShaded)
	{
		Vector3 vector = point1 - point0;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Vector3 center = 0.5f * (point0 + point1);
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Vector3.Dot(vector.normalized, Vector3.up) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCapsule(center, rotation, magnitude, radius, latSegmentsPerCap, longSegmentsPerCap, color, style);
		}
	}

	public static void DrawCone(Vector3 baseCenter, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style = Style.FlatShaded)
	{
		if (height < MathUtil.Epsilon || radius < MathUtil.Epsilon)
		{
			return;
		}
		Mesh mesh = null;
		switch (style)
		{
		case Style.Wireframe:
			mesh = PrimitiveMeshFactory.ConeWireframe(numSegments);
			break;
		case Style.FlatShaded:
			mesh = PrimitiveMeshFactory.ConeFlatShaded(numSegments);
			break;
		case Style.SmoothShaded:
			mesh = PrimitiveMeshFactory.ConeSmoothShaded(numSegments);
			break;
		}
		if (!(mesh == null))
		{
			Gizmos.color = color;
			if (style == Style.Wireframe)
			{
				Gizmos.DrawWireMesh(mesh, baseCenter, rotation, new Vector3(radius, height, radius));
			}
			else
			{
				Gizmos.DrawMesh(mesh, baseCenter, rotation, new Vector3(radius, height, radius));
			}
		}
	}

	public static void DrawCone(Vector3 baseCenter, Vector3 top, float radius, int numSegments, Color color, Style style = Style.FlatShaded)
	{
		Vector3 vector = top - baseCenter;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Vector3.Dot(vector, Vector3.up) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCone(baseCenter, rotation, magnitude, radius, numSegments, color, style);
		}
	}

	public static void DrawArrow(Vector3 from, Vector3 to, float coneRadius, float coneHeight, int numSegments, float stemThickness, Color color, Style style = Style.FlatShaded)
	{
		Vector3 vector = to - from;
		float magnitude = vector.magnitude;
		if (magnitude < MathUtil.Epsilon)
		{
			return;
		}
		vector.Normalize();
		Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Vector3.Dot(vector, Vector3.up) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
		DrawCone(to - coneHeight * vector, rotation, coneHeight, coneRadius, numSegments, color, style);
		if (stemThickness <= 0f)
		{
			if (style != 0)
			{
				to -= coneHeight * vector;
			}
			DrawLine(from, to, color);
		}
		else if (coneHeight < magnitude)
		{
			to -= coneHeight * vector;
			DrawCylinder(from, to, 0.5f * stemThickness, numSegments, color, style);
		}
	}

	public static void DrawArrow(Vector3 from, Vector3 to, float size, Color color, Style style = Style.FlatShaded)
	{
		DrawArrow(from, to, 0.5f * size, size, 8, 0f, color, style);
	}
}
