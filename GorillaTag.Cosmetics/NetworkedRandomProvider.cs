using System;
using System.Text;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class NetworkedRandomProvider : MonoBehaviour
{
	public enum OutputMode
	{
		Float01,
		Double01,
		FloatRange,
		DoubleRange
	}

	[Header("Time Granularity")]
	[Min(0.01f)]
	[Tooltip("Length of the time bucket (seconds). Within a bucket the pick is fixed; re-rolls next bucket.")]
	[SerializeField]
	private float windowSeconds = 1f;

	[Tooltip("Mix room name into seed so different rooms never collide.")]
	[SerializeField]
	private bool includeRoomNameInSeed = true;

	[Tooltip("Optional - If multiple component live on the same cosmetic, use different salts.")]
	[SerializeField]
	private int objectSalt;

	[Header("Output")]
	[SerializeField]
	private OutputMode outputMode;

	[SerializeField]
	private Vector2 floatRange = new Vector2(0f, 1f);

	[SerializeField]
	private double doubleMin;

	[SerializeField]
	private double doubleMax = 1.0;

	private TransferrableObject parentTransferable;

	private int OwnerID;

	[Header("Debug")]
	[SerializeField]
	private long debugWindow;

	[SerializeField]
	private float debugResult;

	private void Awake()
	{
		if (parentTransferable == null)
		{
			parentTransferable = GetComponentInParent<TransferrableObject>();
		}
	}

	private void OnEnable()
	{
		EnsureOwner();
	}

	private void OnValidate()
	{
		if (windowSeconds < 0.01f)
		{
			windowSeconds = 0.01f;
		}
		if (floatRange.y < floatRange.x)
		{
			ref float x = ref floatRange.x;
			ref float y = ref floatRange.y;
			float y2 = floatRange.y;
			float x2 = floatRange.x;
			x = y2;
			y = x2;
		}
		if (doubleMax < doubleMin)
		{
			double num = doubleMax;
			double num2 = doubleMin;
			doubleMin = num;
			doubleMax = num2;
		}
	}

	private void Update()
	{
		long num = (long)Math.Floor(GetSharedTime() / (double)windowSeconds);
		debugWindow = num;
	}

	private bool ShowFloatRange()
	{
		return outputMode == OutputMode.FloatRange;
	}

	private bool ShowDoubleRange()
	{
		return outputMode == OutputMode.DoubleRange;
	}

	private long GetWindowIndex()
	{
		return (long)Math.Floor(GetSharedTime() / (double)windowSeconds);
	}

	private double GetSharedTime()
	{
		if (PhotonNetwork.InRoom)
		{
			return PhotonNetwork.Time;
		}
		return Time.realtimeSinceStartup;
	}

	private static ulong Mix64(ulong x)
	{
		x += 11400714819323198485uL;
		x = (x ^ (x >> 30)) * 13787848793156543929uL;
		x = (x ^ (x >> 27)) * 10723151780598845931uL;
		x ^= x >> 31;
		return x;
	}

	private static ulong BuildSeed(long windowIndex, int ownerId, int objectSalt, uint roomSalt)
	{
		return (ulong)windowIndex ^ ((ulong)(uint)ownerId << 32) ^ (ulong)((uint)objectSalt * -7046029254386353131L) ^ (ulong)(roomSalt * -3263064605168079213L);
	}

	private static float UnitFloat01(long windowIndex, int ownerId, int objectSalt, uint roomSalt)
	{
		return (float)(uint)(Mix64(BuildSeed(windowIndex, ownerId, objectSalt, roomSalt)) >> 40) * 5.9604645E-08f;
	}

	private static double UnitDouble01(long windowIndex, int ownerId, int objectSalt, uint roomSalt)
	{
		return (double)(Mix64(BuildSeed(windowIndex, ownerId, objectSalt, roomSalt)) >> 11) * 1.1102230246251565E-16;
	}

	public float NextFloat01()
	{
		EnsureOwner();
		return debugResult = UnitFloat01(GetWindowIndex(), roomSalt: includeRoomNameInSeed ? StableHash((!PhotonNetwork.InRoom) ? "no_room" : (PhotonNetwork.CurrentRoom?.Name ?? "no_room")) : 0u, ownerId: OwnerID, objectSalt: objectSalt);
	}

	public float NextFloat(float min, float max)
	{
		float t = NextFloat01();
		if (max < min)
		{
			float num = max;
			float num2 = min;
			min = num;
			max = num2;
		}
		return Mathf.Lerp(min, max, t);
	}

	public double NextDouble(double min, double max)
	{
		EnsureOwner();
		double num = UnitDouble01(GetWindowIndex(), roomSalt: includeRoomNameInSeed ? StableHash((!PhotonNetwork.InRoom) ? "no_room" : (PhotonNetwork.CurrentRoom?.Name ?? "no_room")) : 0u, ownerId: OwnerID, objectSalt: objectSalt);
		if (max < min)
		{
			double num2 = max;
			double num3 = min;
			min = num2;
			max = num3;
		}
		double num4 = min + (max - min) * num;
		debugResult = (float)num4;
		return num4;
	}

	public float GetSelectedAsFloat()
	{
		return outputMode switch
		{
			OutputMode.Double01 => (float)NextDouble(0.0, 1.0), 
			OutputMode.FloatRange => NextFloat(floatRange.x, floatRange.y), 
			OutputMode.DoubleRange => (float)NextDouble(doubleMin, doubleMax), 
			_ => NextFloat01(), 
		};
	}

	public double GetSelectedAsDouble()
	{
		return outputMode switch
		{
			OutputMode.Double01 => NextDouble(0.0, 1.0), 
			OutputMode.FloatRange => NextFloat(floatRange.x, floatRange.y), 
			OutputMode.DoubleRange => NextDouble(doubleMin, doubleMax), 
			_ => NextFloat01(), 
		};
	}

	private static uint StableHash(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0u;
		}
		uint num = 2166136261u;
		for (int i = 0; i < s.Length; i++)
		{
			num ^= s[i];
			num *= 16777619;
		}
		return num;
	}

	private void EnsureOwner()
	{
		if (OwnerID == 0)
		{
			TrySetID();
		}
	}

	private void TrySetID()
	{
		if (parentTransferable == null)
		{
			string s = base.gameObject.scene.name + "/" + GetHierarchyPath(base.transform) + GetType();
			OwnerID = s.GetStaticHash();
		}
		else if (parentTransferable.IsLocalObject())
		{
			PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
			if (instance != null)
			{
				OwnerID = (instance.GetPlayFabPlayerId() + GetType()).GetStaticHash();
			}
		}
		else if (parentTransferable.targetRig != null && parentTransferable.targetRig.creator != null)
		{
			OwnerID = (parentTransferable.targetRig.creator.UserId + GetType()).GetStaticHash();
		}
	}

	private static string GetHierarchyPath(Transform t)
	{
		StringBuilder stringBuilder = new StringBuilder();
		while (t != null)
		{
			stringBuilder.Insert(0, "/" + t.name + "#" + t.GetSiblingIndex());
			t = t.parent;
		}
		return stringBuilder.ToString();
	}
}
