using UnityEngine;

namespace GorillaTag;

public interface PrefabNameSerialized
{
	GameObject gameObject { get; }

	string PrefabName { get; set; }
}
