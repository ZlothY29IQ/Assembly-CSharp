using Unity.Mathematics;
using UnityEngine;

public struct VoxelOperation(Vector3 origin, VoxelAction action)
{
	public int3 origin = (int3)((float3)origin * 256f);

	public OperationType operationType = action.operation;

	public int radius = (int)(action.radius * 256f);

	public int strength = (int)(action.strength * 256f);

	public byte material = action.material;

	public bool IsValid()
	{
		if (radius > 0 && strength > 0)
		{
			if (operationType != OperationType.Add)
			{
				return operationType == OperationType.Subtract;
			}
			return true;
		}
		return false;
	}
}
