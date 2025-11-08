using UnityEngine;

[CreateAssetMenu(fileName = "New AudioMixVarPool", menuName = "ScriptableObjects/AudioMixVarPool", order = 0)]
public class AudioMixVarPool : ScriptableObject
{
	[SerializeField]
	private AudioMixVar[] _vars = new AudioMixVar[0];

	public bool Rent(out AudioMixVar mixVar)
	{
		for (int i = 0; i < _vars.Length; i++)
		{
			if (!_vars[i].taken)
			{
				_vars[i].taken = true;
				mixVar = _vars[i];
				return true;
			}
		}
		mixVar = null;
		return false;
	}

	public void Return(AudioMixVar mixVar)
	{
		if (mixVar != null)
		{
			int num = _vars.IndexOfRef(mixVar);
			if (num != -1)
			{
				_vars[num].taken = false;
			}
		}
	}
}
