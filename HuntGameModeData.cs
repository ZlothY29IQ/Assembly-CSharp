using System;
using Fusion;

[NetworkBehaviourWeaved(43)]
public class HuntGameModeData : FusionGameModeData
{
	[WeaverGenerated]
	[DefaultForProperty("huntdata", 0, 43)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private HuntData _huntdata;

	public override object Data
	{
		get
		{
			return huntdata;
		}
		set
		{
			huntdata = (HuntData)value;
		}
	}

	[Networked]
	[NetworkedWeaved(0, 43)]
	private unsafe HuntData huntdata
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HuntGameModeData.huntdata. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(HuntData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HuntGameModeData.huntdata. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(HuntData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		huntdata = _huntdata;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_huntdata = huntdata;
	}
}
