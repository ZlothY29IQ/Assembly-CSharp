using UnityEngine;

namespace GorillaLocomotion.Climbing;

public class HandHoldXSceneRef : MonoBehaviour
{
	[SerializeField]
	public XSceneRef reference;

	public HandHold target
	{
		get
		{
			if (reference.TryResolve(out HandHold result))
			{
				return result;
			}
			return null;
		}
	}

	public GameObject targetObject
	{
		get
		{
			if (reference.TryResolve(out GameObject result))
			{
				return result;
			}
			return null;
		}
	}
}
