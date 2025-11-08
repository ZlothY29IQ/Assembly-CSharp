using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class WormInApple : MonoBehaviour
{
	[SerializeField]
	private UpdateBlendShapeCosmetic blendShapeCosmetic;

	public UnityEvent OnHandTapped;

	public void OnHandTap()
	{
		if ((bool)blendShapeCosmetic && blendShapeCosmetic.GetBlendValue() > 0.5f)
		{
			OnHandTapped?.Invoke();
		}
	}
}
