using System;

namespace TagEffects;

[Serializable]
public class TagEffectsCombo : IEquatable<TagEffectsCombo>
{
	public TagEffectPack inputA;

	public TagEffectPack inputB;

	bool IEquatable<TagEffectsCombo>.Equals(TagEffectsCombo other)
	{
		if (!(other.inputA == inputA) || !(other.inputB == inputB))
		{
			if (other.inputA == inputB)
			{
				return other.inputB == inputA;
			}
			return false;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		return Equals((TagEffectsCombo)obj);
	}

	public override int GetHashCode()
	{
		return inputA.GetHashCode() * inputB.GetHashCode();
	}
}
