using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class GTSignal
{
	public enum EmitMode
	{
		None = -1,
		Others,
		Targets,
		All,
		Host
	}

	public const byte PHOTON_CODE = 186;

	private static Dictionary<EmitMode, RaiseEventOptions> gTargetsToOptions;

	private static Dictionary<int, object[]> gLengthToContentArray = new Dictionary<int, object[]>
	{
		[1] = new object[1],
		[2] = new object[2],
		[3] = new object[3],
		[4] = new object[4],
		[5] = new object[5],
		[6] = new object[6],
		[7] = new object[7],
		[8] = new object[8],
		[9] = new object[9],
		[10] = new object[10],
		[11] = new object[11],
		[12] = new object[12],
		[13] = new object[13],
		[14] = new object[14],
		[15] = new object[15],
		[16] = new object[16]
	};

	private static Dictionary<int, int[]> gLengthToTargetsArray = new Dictionary<int, int[]>
	{
		[1] = new int[1],
		[2] = new int[2],
		[3] = new int[3],
		[4] = new int[4],
		[5] = new int[5],
		[6] = new int[6],
		[7] = new int[7],
		[8] = new int[8],
		[9] = new int[9],
		[10] = new int[10]
	};

	private static SendOptions gSendOptions;

	private static RaiseEventOptions gCustomTargetOptions;

	private static void _Emit(EmitMode mode, int signalID, object[] data)
	{
		object[] eventContent = _ToEventContent(signalID, PhotonNetwork.Time, data);
		PhotonNetwork.RaiseEvent(186, eventContent, gTargetsToOptions[mode], gSendOptions);
	}

	private static void _Emit(int[] targetActors, int signalID, object[] data)
	{
		if (!targetActors.IsNullOrEmpty())
		{
			gCustomTargetOptions.TargetActors = targetActors;
			object[] eventContent = _ToEventContent(signalID, PhotonNetwork.Time, data);
			PhotonNetwork.RaiseEvent(186, eventContent, gCustomTargetOptions, gSendOptions);
		}
	}

	private static object[] _ToEventContent(int signalID, double time, object[] data)
	{
		int num = data.Length;
		int num2 = num + 2;
		if (!gLengthToContentArray.TryGetValue(num2, out var value))
		{
			value = new object[num2];
			gLengthToContentArray.Add(num2, value);
		}
		value[0] = signalID;
		value[1] = time;
		for (int i = 0; i < num; i++)
		{
			value[i + 2] = data[i];
		}
		return value;
	}

	public static int ComputeID(string s)
	{
		if (!string.IsNullOrWhiteSpace(s))
		{
			return XXHash32.Compute(s.Trim());
		}
		return 0;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitializeOnLoad()
	{
		gCustomTargetOptions = RaiseEventOptions.Default;
		gSendOptions = SendOptions.SendReliable;
		gSendOptions.Encrypt = true;
		gTargetsToOptions = new Dictionary<EmitMode, RaiseEventOptions>(3);
		RaiseEventOptions raiseEventOptions = RaiseEventOptions.Default;
		raiseEventOptions.Receivers = ReceiverGroup.All;
		gTargetsToOptions.Add(EmitMode.All, raiseEventOptions);
		RaiseEventOptions raiseEventOptions2 = RaiseEventOptions.Default;
		raiseEventOptions2.Receivers = ReceiverGroup.Others;
		gTargetsToOptions.Add(EmitMode.Others, raiseEventOptions2);
		RaiseEventOptions raiseEventOptions3 = RaiseEventOptions.Default;
		raiseEventOptions3.Receivers = ReceiverGroup.MasterClient;
		gTargetsToOptions.Add(EmitMode.Host, raiseEventOptions3);
	}

	public static void Emit(string signal, params object[] data)
	{
		_Emit(EmitMode.All, ComputeID(signal), data);
	}

	public static void Emit(EmitMode mode, string signal, params object[] data)
	{
		_Emit(mode, ComputeID(signal), data);
	}

	public static void Emit(int signalID, params object[] data)
	{
		_Emit(EmitMode.All, signalID, data);
	}

	public static void Emit(EmitMode mode, int signalID, params object[] data)
	{
		_Emit(mode, signalID, data);
	}

	public static void Emit(int target, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[1];
		array[0] = target;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[2];
		array[0] = target1;
		array[1] = target2;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[3];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[4];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[5];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int target6, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[6];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		array[5] = target6;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int target6, int target7, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[7];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		array[5] = target6;
		array[6] = target7;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int target6, int target7, int target8, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[8];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		array[5] = target6;
		array[6] = target7;
		array[7] = target8;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int target6, int target7, int target8, int target9, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[9];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		array[5] = target6;
		array[6] = target7;
		array[7] = target8;
		array[8] = target9;
		_Emit(array, signalID, data);
	}

	public static void Emit(int target1, int target2, int target3, int target4, int target5, int target6, int target7, int target8, int target9, int target10, int signalID, params object[] data)
	{
		int[] array = gLengthToTargetsArray[10];
		array[0] = target1;
		array[1] = target2;
		array[2] = target3;
		array[3] = target4;
		array[4] = target5;
		array[5] = target6;
		array[6] = target7;
		array[7] = target8;
		array[8] = target9;
		array[9] = target10;
		_Emit(array, signalID, data);
	}
}
