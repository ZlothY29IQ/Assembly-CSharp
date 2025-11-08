using UnityEngine;

namespace BoingKit;

[CreateAssetMenu(fileName = "BoingParams", menuName = "Boing Kit/Shared Boing Params", order = 550)]
public class SharedBoingParams : ScriptableObject
{
	public BoingWork.Params Params;

	public SharedBoingParams()
	{
		Params.Init();
	}
}
