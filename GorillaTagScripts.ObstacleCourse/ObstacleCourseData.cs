using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

[StructLayout(LayoutKind.Explicit, Size = 36)]
[NetworkStructWeaved(9)]
public struct ObstacleCourseData : INetworkStruct
{
	[FieldOffset(4)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 4, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00404 _WinnerActorNumber;

	[FieldOffset(20)]
	[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 4, order = -2147483647)]
	[WeaverGenerated]
	[SerializeField]
	private FixedStorage_00404 _CurrentRaceState;

	[field: FieldOffset(0)]
	public int ObstacleCourseCount { get; set; }

	[Networked]
	[Capacity(4)]
	[NetworkedWeavedArray(4, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(1, 4)]
	public unsafe NetworkArray<int> WinnerActorNumber => new NetworkArray<int>(Native.ReferenceToPointer(ref _WinnerActorNumber), 4, Fusion.ElementReaderWriterInt32.GetInstance());

	[Networked]
	[Capacity(4)]
	[NetworkedWeavedArray(4, 1, typeof(Fusion.ElementReaderWriterInt32))]
	[NetworkedWeaved(5, 4)]
	public unsafe NetworkArray<int> CurrentRaceState => new NetworkArray<int>(Native.ReferenceToPointer(ref _CurrentRaceState), 4, Fusion.ElementReaderWriterInt32.GetInstance());

	public ObstacleCourseData(List<ObstacleCourse> courses)
	{
		ObstacleCourseCount = courses.Count;
		int[] array = new int[ObstacleCourseCount];
		int[] array2 = new int[ObstacleCourseCount];
		for (int i = 0; i < courses.Count; i++)
		{
			array[i] = courses[i].winnerActorNumber;
			array2[i] = (int)courses[i].currentState;
		}
		WinnerActorNumber.CopyFrom(array, 0, ObstacleCourseCount);
		CurrentRaceState.CopyFrom(array2, 0, ObstacleCourseCount);
	}
}
