using System;
using Fusion;

[NetworkBehaviourWeaved(22)]
public class TagGameModeData : FusionGameModeData
{
	[WeaverGenerated]
	[DefaultForProperty("tagData", 0, 22)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private TagData _tagData;

	public override object Data
	{
		get
		{
			return tagData;
		}
		set
		{
			tagData = (TagData)value;
		}
	}

	[Networked]
	[NetworkedWeaved(0, 22)]
	private unsafe TagData tagData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TagGameModeData.tagData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(TagData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TagGameModeData.tagData. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(TagData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		tagData = _tagData;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_tagData = tagData;
	}
}
