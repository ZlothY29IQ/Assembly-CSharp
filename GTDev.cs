using System;
using System.Diagnostics;
using Cysharp.Text;
using Drawing;
using UnityEngine;

public static class GTDev
{
	[OnEnterPlay_Set(0)]
	private static int gDevID;

	[OnEnterPlay_Set(false)]
	private static bool gHasDevID;

	private static readonly Color gDefaultColor = new Color(0f, 1f, 1f, 0.32f);

	private const string kFormatF = "{{ X: {0:##0.0000}, Y: {1:##0.0000}, Z: {2:##0.0000} }}";

	private const float kDuration = 8f;

	private static Mesh gSphereMesh;

	public static int DevID => FetchDevID();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
	private static void InitializeOnLoad()
	{
		FetchDevID();
	}

	[HideInCallstack]
	public static void Log<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	public static void Log<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogError<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogError<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogWarning<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogWarning<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogSilent<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	public static void LogSilent<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void LogEditorOnly<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void LogEditorOnly<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogBetaOnly<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogBetaOnly<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void LogErrorEd<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void LogErrorEd<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogErrorBeta<T>(T msg, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("BETA")]
	[Conditional("UNITY_EDITOR")]
	public static void LogErrorBeta<T>(T msg, UnityEngine.Object context, string channel = null)
	{
	}

	[HideInCallstack]
	[Conditional("UNITY_EDITOR")]
	public static void CallEditorOnly(Action call)
	{
	}

	private static int FetchDevID()
	{
		if (gHasDevID)
		{
			return gDevID;
		}
		int i = StaticHash.Compute(SystemInfo.deviceUniqueIdentifier);
		int i2 = StaticHash.Compute(Environment.UserDomainName);
		int i3 = StaticHash.Compute(Environment.UserName);
		int i4 = StaticHash.Compute(Application.unityVersion);
		gDevID = StaticHash.Compute(i, i2, i3, i4);
		gHasDevID = true;
		return gDevID;
	}

	[HideInCallstack]
	[Conditional("_GTDEV_ON_")]
	private static void _Log<T>(Action<object, UnityEngine.Object> log, Action<object> logNoCtx, T msg, UnityEngine.Object ctx, string channel)
	{
	}

	private static Mesh SphereMesh()
	{
		if (!gSphereMesh)
		{
			gSphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
		}
		return gSphereMesh;
	}

	[Conditional("_GTDEV_ON_")]
	public static void Ping3D(this Collider col, Color color = default(Color), float duration = 8f)
	{
		if (color == default(Color))
		{
			color = gDefaultColor;
		}
		if (color.a.Approx0())
		{
			return;
		}
		Matrix4x4 localToWorldMatrix = col.transform.localToWorldMatrix;
		SRand sRand = new SRand(localToWorldMatrix.QuantizedId128().GetHashCode());
		color.r = sRand.NextFloat();
		color.g = sRand.NextFloat();
		color.b = sRand.NextFloat();
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithDuration(duration))
		{
			ingame.PushMatrix(localToWorldMatrix);
			ingame.PushLineWidth(2f);
			ingame.PushColor(color);
			if (!(col is BoxCollider boxCollider))
			{
				if (!(col is SphereCollider sphereCollider))
				{
					if (col is CapsuleCollider capsuleCollider)
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
				ingame.WireBox(boxCollider.center, boxCollider.size);
			}
			ingame.Label2D(Vector3.zero, col.name, 16f, LabelAlignment.Center);
			ingame.PopColor();
			ingame.PopLineWidth();
			ingame.PopMatrix();
		}
	}

	[Conditional("_GTDEV_ON_")]
	public static void Ping3D(this Vector3 vec, Color color = default(Color), float duration = 8f)
	{
		if (color == default(Color))
		{
			color = gDefaultColor;
		}
		else
		{
			color.a = gDefaultColor.a;
		}
		string text = ZString.Format("{{ X: {0:##0.0000}, Y: {1:##0.0000}, Z: {2:##0.0000} }}", vec.x, vec.y, vec.z);
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithDuration(duration))
		{
			using (ingame.WithLineWidth(2f))
			{
				ingame.Cross(vec, 0.64f, color);
			}
			ingame.Label2D(vec + Vector3.down * 0.64f, text, 16f, LabelAlignment.Center, color);
		}
	}

	[Conditional("_GTDEV_ON_")]
	public static void Ping3D<T>(this T value, Vector3 position, Color color = default(Color), float duration = 8f)
	{
		if (color == default(Color))
		{
			color = gDefaultColor;
		}
		string text = ZString.Concat(value);
		CommandBuilder ingame = Draw.ingame;
		using (ingame.WithDuration(duration))
		{
			ingame.Label2D(position, text, 16f, LabelAlignment.Center, color);
		}
	}
}
