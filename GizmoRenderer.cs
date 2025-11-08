using System;
using Drawing;
using Unity.Mathematics;
using UnityEngine;

public class GizmoRenderer : MonoBehaviour
{
	[Serializable]
	public class GizmoInfo
	{
		public bool render = true;

		public GizmoType type;

		public Color color = GetRandomColor();

		public uint lineWidth = 1u;

		[Space]
		public Transform target;

		[Space]
		public float3 center = float3.zero;

		public float3 size = Vector3.one;

		public float radius = 1f;

		public quaternion rotation = quaternion.identity;

		[Space]
		public string text = string.Empty;

		public float textSize = 4f;

		public TextAlign textAlign;

		public uint textPPU = 24u;

		[Space]
		public int2 gridCells = new int2(4);
	}

	[Flags]
	public enum RenderMode : uint
	{
		Never = 0u,
		InEditor = 1u,
		InBuild = 2u,
		Always = 3u
	}

	public enum GizmoType : uint
	{
		BoxWire,
		BoxSolid,
		SphereWire,
		SphereSolid,
		Label3D,
		Label2D,
		GridWire,
		PlaneSolid,
		PlaneWire
	}

	public enum TextAlign : uint
	{
		Center,
		MiddleRight,
		MiddleLeft,
		BottomCenter,
		BottomRight,
		BottomLeft,
		TopRight,
		TopLeft,
		TopCenter
	}

	public RenderMode renderMode = RenderMode.Always;

	public bool includeInBuild;

	public GizmoInfo[] gizmos = new GizmoInfo[0];

	private static readonly Action<CommandBuilder, GizmoInfo>[] gRenderFuncs = new Action<CommandBuilder, GizmoInfo>[9] { RenderBoxWire, RenderBoxSolid, RenderSphereWire, RenderSphereSolid, RenderLabel3D, RenderLabel2D, RenderGridWire, RenderPlaneSolid, RenderPlaneWire };

	private static readonly LabelAlignment[] gLabelAligns = new LabelAlignment[9]
	{
		LabelAlignment.Center,
		LabelAlignment.MiddleRight,
		LabelAlignment.MiddleLeft,
		LabelAlignment.BottomCenter,
		LabelAlignment.BottomRight,
		LabelAlignment.BottomLeft,
		LabelAlignment.TopRight,
		LabelAlignment.TopLeft,
		LabelAlignment.TopCenter
	};

	private static Mesh gSphereMesh;

	private void Update()
	{
		RenderGizmos();
	}

	private void RenderGizmos()
	{
		if (renderMode == RenderMode.Never || gizmos == null)
		{
			return;
		}
		int num = gizmos.Length;
		if (num == 0)
		{
			return;
		}
		CommandBuilder ingame = Draw.ingame;
		Transform transform = base.transform;
		for (int i = 0; i < num; i++)
		{
			GizmoInfo gizmoInfo = gizmos[i];
			if (!gizmoInfo.render)
			{
				continue;
			}
			Transform transform2 = (gizmoInfo.target ? gizmoInfo.target : transform);
			using (ingame.InLocalSpace(transform2))
			{
				using (ingame.WithLineWidth(gizmoInfo.lineWidth, automaticJoins: false))
				{
					gRenderFuncs[(uint)gizmoInfo.type](ingame, gizmoInfo);
				}
			}
		}
	}

	private static void RenderPlaneWire(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.WirePlane(gizmo.center, gizmo.rotation, gizmo.size.xz, gizmo.color);
	}

	private static void RenderPlaneSolid(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.SolidPlane(gizmo.center, gizmo.rotation, gizmo.size.xz, gizmo.color);
	}

	private static void RenderGridWire(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.WireGrid(gizmo.center, gizmo.rotation, gizmo.gridCells, gizmo.size.xz, gizmo.color);
	}

	private static void RenderBoxWire(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.WireBox(gizmo.center, gizmo.rotation, gizmo.size, gizmo.color);
	}

	private static void RenderBoxSolid(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.SolidBox(gizmo.center, gizmo.rotation, gizmo.size, gizmo.color);
	}

	private static void RenderSphereWire(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.WireSphere(gizmo.center, gizmo.radius * 0.5f, gizmo.color);
	}

	private static void RenderSphereSolid(CommandBuilder draw, GizmoInfo gizmo)
	{
		Matrix4x4 matrix = Matrix4x4.TRS(gizmo.center, quaternion.identity, new float3(gizmo.radius));
		using (draw.WithMatrix(matrix))
		{
			draw.SolidMesh(gSphereMesh, gizmo.color);
		}
	}

	private static void RenderLabel3D(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.Label3D(gizmo.center, gizmo.rotation, gizmo.text, gizmo.textSize * 0.1f, gLabelAligns[(uint)gizmo.textAlign], gizmo.color);
	}

	private static void RenderLabel2D(CommandBuilder draw, GizmoInfo gizmo)
	{
		draw.Label2D(gizmo.center, gizmo.text, gizmo.textSize * (float)gizmo.textPPU, gLabelAligns[(uint)gizmo.textAlign], gizmo.color);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitializeOnLoad()
	{
		gSphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
	}

	private static Color GetRandomColor()
	{
		Color result = Color.HSVToRGB((float)(DateTime.UtcNow.Ticks % 65536) / 65535f, 1f, 1f, hdr: true);
		result.a = 1f;
		return result;
	}
}
