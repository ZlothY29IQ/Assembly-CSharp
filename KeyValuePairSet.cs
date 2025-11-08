using UnityEngine;

[CreateAssetMenu(fileName = "New KeyValuePairSet", menuName = "Data/KeyValuePairSet", order = 0)]
public class KeyValuePairSet : ScriptableObject
{
	[SerializeField]
	private KeyValueStringPair[] m_entries;

	public KeyValueStringPair[] Entries => m_entries;
}
