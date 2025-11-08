using System.Collections.Generic;
using UnityEngine;

namespace CjLib;

public class DebugUtil
{
	public enum Style
	{
		Wireframe,
		SolidColor,
		FlatShaded,
		SmoothShaded
	}

	private static float s_wireframeZBias = 0.0001f;

	private const int kNormalFlag = 1;

	private const int kCapShiftScaleFlag = 2;

	private const int kDepthTestFlag = 4;

	private static Dictionary<int, Material> s_materialPool;

	private static MaterialPropertyBlock s_materialProperties;

	private static Material GetMaterial(Style style, bool depthTest, bool capShiftScale)
	{
		int num = 0;
		if ((uint)(style - 2) <= 1u)
		{
			num |= 1;
		}
		if (capShiftScale)
		{
			num |= 2;
		}
		if (depthTest)
		{
			num |= 4;
		}
		if (s_materialPool == null)
		{
			s_materialPool = new Dictionary<int, Material>();
		}
		if (!s_materialPool.TryGetValue(num, out var value) || value == null)
		{
			if (value == null)
			{
				s_materialPool.Remove(num);
			}
			Shader shader = Shader.Find(depthTest ? "CjLib/Primitive" : "CjLib/PrimitiveNoZTest");
			if (shader == null)
			{
				return null;
			}
			value = new Material(shader);
			if ((num & 1) != 0)
			{
				value.EnableKeyword("NORMAL_ON");
			}
			if ((num & 2) != 0)
			{
				value.EnableKeyword("CAP_SHIFT_SCALE");
			}
			s_materialPool.Add(num, value);
		}
		return value;
	}

	private static MaterialPropertyBlock GetMaterialPropertyBlock()
	{
		if (s_materialProperties == null)
		{
			return s_materialProperties = new MaterialPropertyBlock();
		}
		return s_materialProperties;
	}

	public static void DrawLine(Vector3 v0, Vector3 v1, Color color, bool depthTest = true)
	{
		Mesh mesh = PrimitiveMeshFactory.Line(v0, v1);
		if (!(mesh == null))
		{
			Material material = GetMaterial(Style.Wireframe, depthTest, capShiftScale: false);
			MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
			materialPropertyBlock.SetColor("_Color", color);
			materialPropertyBlock.SetVector("_Dimensions", new Vector4(1f, 1f, 1f, 0f));
			materialPropertyBlock.SetFloat("_ZBias", s_wireframeZBias);
			Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
		}
	}

	public static void DrawLines(Vector3[] aVert, Color color, bool depthTest = true)
	{
		Mesh mesh = PrimitiveMeshFactory.Lines(aVert);
		if (!(mesh == null))
		{
			Material material = GetMaterial(Style.Wireframe, depthTest, capShiftScale: false);
			MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
			materialPropertyBlock.SetColor("_Color", color);
			materialPropertyBlock.SetVector("_Dimensions", new Vector4(1f, 1f, 1f, 0f));
			materialPropertyBlock.SetFloat("_ZBias", s_wireframeZBias);
			Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
		}
	}

	public static void DrawLineStrip(Vector3[] aVert, Color color, bool depthTest = true)
	{
		Mesh mesh = PrimitiveMeshFactory.LineStrip(aVert);
		if (!(mesh == null))
		{
			Material material = GetMaterial(Style.Wireframe, depthTest, capShiftScale: false);
			MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
			materialPropertyBlock.SetColor("_Color", color);
			materialPropertyBlock.SetVector("_Dimensions", new Vector4(1f, 1f, 1f, 0f));
			materialPropertyBlock.SetFloat("_ZBias", s_wireframeZBias);
			Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
		}
	}

	public static void DrawArc(Vector3 center, Vector3 from, Vector3 normal, float angle, float radius, int numSegments, Color color, bool depthTest = true)
	{
		if (numSegments > 0)
		{
			from.Normalize();
			from *= radius;
			Vector3[] array = new Vector3[numSegments + 1];
			array[0] = center + from;
			float num = 1f / (float)numSegments;
			Quaternion quaternion = QuaternionUtil.AxisAngle(normal, angle * num);
			Vector3 vector = quaternion * from;
			for (int i = 1; i <= numSegments; i++)
			{
				array[i] = center + vector;
				vector = quaternion * vector;
			}
			DrawLineStrip(array, color, depthTest);
		}
	}

	public static void DrawLocator(Vector3 position, Vector3 right, Vector3 up, Vector3 forward, Color rightColor, Color upColor, Color forwardColor, float size = 0.5f)
	{
		DrawLine(position, position + right * size, rightColor);
		DrawLine(position, position + up * size, upColor);
		DrawLine(position, position + forward * size, forwardColor);
	}

	public static void DrawLocator(Vector3 position, Vector3 right, Vector3 up, Vector3 forward, float size = 0.5f)
	{
		DrawLocator(position, right, up, forward, Color.red, Color.green, Color.blue, size);
	}

	public static void DrawLocator(Vector3 position, Quaternion rotation, Color rightColor, Color upColor, Color forwardColor, float size = 0.5f)
	{
		Vector3 right = rotation * Vector3.right;
		Vector3 up = rotation * Vector3.up;
		Vector3 forward = rotation * Vector3.forward;
		DrawLocator(position, right, up, forward, rightColor, upColor, forwardColor, size);
	}

	public static void DrawLocator(Vector3 position, Quaternion rotation, float size = 0.5f)
	{
		DrawLocator(position, rotation, Color.red, Color.green, Color.blue, size);
	}

	public static void DrawBox(Vector3 center, Quaternion rotation, Vector3 dimensions, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(dimensions.x < MathUtil.Epsilon) && !(dimensions.y < MathUtil.Epsilon) && !(dimensions.z < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.BoxWireframe();
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.BoxSolidColor();
				break;
			case Style.FlatShaded:
			case Style.SmoothShaded:
				mesh = PrimitiveMeshFactory.BoxFlatShaded();
				break;
			}
			if (!(mesh == null))
			{
				Material material = GetMaterial(style, depthTest, capShiftScale: false);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(dimensions.x, dimensions.y, dimensions.z, 0f));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawRect(Vector3 center, Quaternion rotation, Vector2 dimensions, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(dimensions.x < MathUtil.Epsilon) && !(dimensions.y < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.RectWireframe();
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.RectSolidColor();
				break;
			case Style.FlatShaded:
			case Style.SmoothShaded:
				mesh = PrimitiveMeshFactory.RectFlatShaded();
				break;
			}
			if (!(mesh == null))
			{
				Material material = GetMaterial(style, depthTest, capShiftScale: false);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(dimensions.x, 1f, dimensions.y, 0f));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawRect2D(Vector3 center, float rotationDeg, Vector2 dimensions, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Quaternion rotation = Quaternion.AngleAxis(rotationDeg, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.right);
		DrawRect(center, rotation, dimensions, color, depthTest, style);
	}

	public static void DrawCircle(Vector3 center, Quaternion rotation, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.CircleWireframe(numSegments);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.CircleSolidColor(numSegments);
				break;
			case Style.FlatShaded:
			case Style.SmoothShaded:
				mesh = PrimitiveMeshFactory.CircleFlatShaded(numSegments);
				break;
			}
			if (!(mesh == null))
			{
				Material material = GetMaterial(style, depthTest, capShiftScale: false);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, radius, radius, 0f));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawCircle(Vector3 center, Vector3 normal, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.5f) ? Vector3.up : Vector3.forward, normal)), normal);
		DrawCircle(center, rotation, radius, numSegments, color, depthTest, style);
	}

	public static void DrawCircle2D(Vector3 center, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		DrawCircle(center, Vector3.forward, radius, numSegments, color, depthTest, style);
	}

	public static void DrawCylinder(Vector3 center, Quaternion rotation, float height, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(height < MathUtil.Epsilon) && !(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.CylinderWireframe(numSegments);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.CylinderSolidColor(numSegments);
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
				Material material = GetMaterial(style, depthTest, capShiftScale: true);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, radius, radius, height));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawCylinder(Vector3 point0, Vector3 point1, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Vector3 vector = point1 - point0;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Vector3 center = 0.5f * (point0 + point1);
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Mathf.Abs(Vector3.Dot(vector.normalized, Vector3.up)) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCylinder(center, rotation, magnitude, radius, numSegments, color, depthTest, style);
		}
	}

	public static void DrawSphere(Vector3 center, Quaternion rotation, float radius, int latSegments, int longSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.SphereWireframe(latSegments, longSegments);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.SphereSolidColor(latSegments, longSegments);
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
				Material material = GetMaterial(style, depthTest, capShiftScale: false);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, radius, radius, 0f));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawSphere(Vector3 center, float radius, int latSegments, int longSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		DrawSphere(center, Quaternion.identity, radius, latSegments, longSegments, color, depthTest, style);
	}

	public static void DrawSphereTripleCircles(Vector3 center, Quaternion rotation, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Vector3 normal = rotation * Vector3.right;
		Vector3 normal2 = rotation * Vector3.up;
		Vector3 normal3 = rotation * Vector3.forward;
		DrawCircle(center, normal, radius, numSegments, color, depthTest, style);
		DrawCircle(center, normal2, radius, numSegments, color, depthTest, style);
		DrawCircle(center, normal3, radius, numSegments, color, depthTest, style);
	}

	public static void DrawSphereTripleCircles(Vector3 center, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		DrawSphereTripleCircles(center, Quaternion.identity, radius, numSegments, color, depthTest, style);
	}

	public static void DrawCapsule(Vector3 center, Quaternion rotation, float height, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(height < MathUtil.Epsilon) && !(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.CapsuleWireframe(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.CapsuleSolidColor(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
				break;
			case Style.FlatShaded:
				mesh = PrimitiveMeshFactory.CapsuleFlatShaded(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
				break;
			case Style.SmoothShaded:
				mesh = PrimitiveMeshFactory.CapsuleSmoothShaded(latSegmentsPerCap, longSegmentsPerCap, caps: true, topCapOnly: true, sides: false);
				break;
			}
			if (!(mesh == null))
			{
				Material material = GetMaterial(style, depthTest, capShiftScale: true);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, radius, radius, height));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawCapsule(Vector3 point0, Vector3 point1, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Vector3 vector = point1 - point0;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Vector3 center = 0.5f * (point0 + point1);
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Mathf.Abs(Vector3.Dot(vector.normalized, Vector3.up)) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCapsule(center, rotation, magnitude, radius, latSegmentsPerCap, longSegmentsPerCap, color, depthTest, style);
		}
	}

	public static void DrawCapsule2D(Vector3 center, float rotationDeg, float height, float radius, int capSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(height < MathUtil.Epsilon) && !(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.Capsule2DWireframe(capSegments);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.Capsule2DSolidColor(capSegments);
				break;
			case Style.FlatShaded:
			case Style.SmoothShaded:
				mesh = PrimitiveMeshFactory.Capsule2DFlatShaded(capSegments);
				break;
			}
			if (!(mesh == null))
			{
				Material material = GetMaterial(style, depthTest, capShiftScale: true);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, radius, radius, height));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, center, Quaternion.AngleAxis(rotationDeg, Vector3.forward), material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawCone(Vector3 baseCenter, Quaternion rotation, float height, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		if (!(height < MathUtil.Epsilon) && !(radius < MathUtil.Epsilon))
		{
			Mesh mesh = null;
			switch (style)
			{
			case Style.Wireframe:
				mesh = PrimitiveMeshFactory.ConeWireframe(numSegments);
				break;
			case Style.SolidColor:
				mesh = PrimitiveMeshFactory.ConeSolidColor(numSegments);
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
				Material material = GetMaterial(style, depthTest, capShiftScale: false);
				MaterialPropertyBlock materialPropertyBlock = GetMaterialPropertyBlock();
				materialPropertyBlock.SetColor("_Color", color);
				materialPropertyBlock.SetVector("_Dimensions", new Vector4(radius, height, radius, 0f));
				materialPropertyBlock.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0f);
				Graphics.DrawMesh(mesh, baseCenter, rotation, material, 0, null, 0, materialPropertyBlock, castShadows: false, receiveShadows: false, useLightProbes: false);
			}
		}
	}

	public static void DrawCone(Vector3 baseCenter, Vector3 top, float radius, int numSegments, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Vector3 vector = top - baseCenter;
		float magnitude = vector.magnitude;
		if (!(magnitude < MathUtil.Epsilon))
		{
			vector.Normalize();
			Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Mathf.Abs(Vector3.Dot(vector, Vector3.up)) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
			DrawCone(baseCenter, rotation, magnitude, radius, numSegments, color, depthTest, style);
		}
	}

	public static void DrawArrow(Vector3 from, Vector3 to, float coneRadius, float coneHeight, int numSegments, float stemThickness, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		Vector3 vector = to - from;
		float magnitude = vector.magnitude;
		if (magnitude < MathUtil.Epsilon)
		{
			return;
		}
		vector.Normalize();
		Quaternion rotation = Quaternion.LookRotation(Vector3.Normalize(Vector3.Cross((Mathf.Abs(Vector3.Dot(vector, Vector3.up)) < 0.5f) ? Vector3.up : Vector3.forward, vector)), vector);
		DrawCone(to - coneHeight * vector, rotation, coneHeight, coneRadius, numSegments, color, depthTest, style);
		if (stemThickness <= 0f)
		{
			if (style == Style.Wireframe)
			{
				to -= coneHeight * vector;
			}
			DrawLine(from, to, color, depthTest);
		}
		else if (coneHeight < magnitude)
		{
			to -= coneHeight * vector;
			DrawCylinder(from, to, 0.5f * stemThickness, numSegments, color, depthTest, style);
		}
	}

	public static void DrawArrow(Vector3 from, Vector3 to, float size, Color color, bool depthTest = true, Style style = Style.Wireframe)
	{
		DrawArrow(from, to, 0.5f * size, size, 8, 0f, color, depthTest, style);
	}
}
