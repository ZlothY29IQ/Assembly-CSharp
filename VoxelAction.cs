using System;

[Serializable]
public struct VoxelAction(OperationType operation, float radius, float strength, byte material = 0)
{
	public OperationType operation = operation;

	public float radius = radius;

	public float strength = strength;

	public byte material = material;

	public bool IsValid()
	{
		if (float.IsFinite(radius) && radius > 0f && float.IsFinite(strength) && strength > 0f)
		{
			if (operation != OperationType.Add)
			{
				return operation == OperationType.Subtract;
			}
			return true;
		}
		return false;
	}
}
