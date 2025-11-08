using System;
using UnityEngine;

public class EmitSignalToBiter : GTSignalEmitter
{
	[Flags]
	private enum EdibleState
	{
		None = 0,
		State0 = 1,
		State1 = 2,
		State2 = 4,
		State3 = 8
	}

	[Space]
	public EdibleHoldable targetEdible;

	[Space]
	[SerializeField]
	private EdibleState onEdibleState;

	public override void Emit()
	{
		if (onEdibleState == EdibleState.None || !targetEdible || targetEdible.lastBiterActorID == -1)
		{
			return;
		}
		TransferrableObject.ItemStates itemState = targetEdible.itemState;
		if ((uint)(itemState - 1) <= 1u || itemState == TransferrableObject.ItemStates.State2 || itemState == TransferrableObject.ItemStates.State3)
		{
			int num = (int)itemState;
			if (((uint)onEdibleState & (uint)num) == (uint)num)
			{
				GTSignal.Emit(targetEdible.lastBiterActorID, signal);
			}
		}
	}

	public override void Emit(int targetActor)
	{
	}

	public override void Emit(params object[] data)
	{
	}
}
