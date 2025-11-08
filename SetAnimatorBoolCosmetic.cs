using UnityEngine;

public class SetAnimatorBoolCosmetic : MonoBehaviour
{
	[SerializeField]
	private Animator animator;

	[SerializeField]
	private string boolParameterName;

	[SerializeField]
	private string bool2ParameterName;

	[SerializeField]
	private string bool3ParameterName;

	[SerializeField]
	private string bool4ParameterName;

	[SerializeField]
	private string bool5ParameterName;

	[SerializeField]
	private string int1ParameterName;

	[SerializeField]
	private string int2ParameterName;

	[SerializeField]
	private string int3ParameterName;

	[SerializeField]
	private string int4ParameterName;

	[SerializeField]
	private string float1ParameterName;

	[SerializeField]
	private string float2ParameterName;

	[SerializeField]
	private string float3ParameterName;

	[SerializeField]
	private string float4ParameterName;

	private int bool1Hash;

	private int bool2Hash;

	private int bool3Hash;

	private int bool4Hash;

	private int bool5Hash;

	private const int MAX_BOOLS = 5;

	private int int1Hash;

	private int int2Hash;

	private int int3Hash;

	private int int4Hash;

	private const int MAX_INTS = 4;

	private int float1Hash;

	private int float2Hash;

	private int float3Hash;

	private int float4Hash;

	private const int MAX_FLOATS = 4;

	private void OnAnimatorValueChanged()
	{
	}

	public void SetAnimatorBool(bool value)
	{
		if (bool1Hash == 0)
		{
			bool1Hash = Animator.StringToHash(boolParameterName);
		}
		animator.SetBool(bool1Hash, value);
	}

	public void SetAnimatorBool2(bool value)
	{
		if (bool2Hash == 0)
		{
			bool2Hash = Animator.StringToHash(bool2ParameterName);
		}
		animator.SetBool(bool2Hash, value);
	}

	public void SetAnimatorBool3(bool value)
	{
		if (bool3Hash == 0)
		{
			bool3Hash = Animator.StringToHash(bool3ParameterName);
		}
		animator.SetBool(bool3Hash, value);
	}

	public void SetAnimatorBool4(bool value)
	{
		if (bool4Hash == 0)
		{
			bool4Hash = Animator.StringToHash(bool4ParameterName);
		}
		animator.SetBool(bool4Hash, value);
	}

	public void SetAnimatorBool5(bool value)
	{
		if (bool5Hash == 0)
		{
			bool5Hash = Animator.StringToHash(bool5ParameterName);
		}
		animator.SetBool(bool5Hash, value);
	}

	public void SetAnimatorInteger1(int value)
	{
		if (int1Hash == 0)
		{
			int1Hash = Animator.StringToHash(int1ParameterName);
		}
		animator.SetInteger(int1Hash, value);
	}

	public void SetAnimatorInteger2(int value)
	{
		if (int2Hash == 0)
		{
			int2Hash = Animator.StringToHash(int2ParameterName);
		}
		animator.SetInteger(int2Hash, value);
	}

	public void SetAnimatorInteger3(int value)
	{
		if (int3Hash == 0)
		{
			int3Hash = Animator.StringToHash(int3ParameterName);
		}
		animator.SetInteger(int3Hash, value);
	}

	public void SetAnimatorInteger4(int value)
	{
		if (int4Hash == 0)
		{
			int4Hash = Animator.StringToHash(int4ParameterName);
		}
		animator.SetInteger(int4Hash, value);
	}

	public void SetAnimatorFloat1(float value)
	{
		if (float1Hash == 0)
		{
			float1Hash = Animator.StringToHash(float1ParameterName);
		}
		animator.SetFloat(float1Hash, value);
	}

	public void SetAnimatorFloat2(float value)
	{
		if (float2Hash == 0)
		{
			float2Hash = Animator.StringToHash(float2ParameterName);
		}
		animator.SetFloat(float2Hash, value);
	}

	public void SetAnimatorFloat3(float value)
	{
		if (float3Hash == 0)
		{
			float3Hash = Animator.StringToHash(float3ParameterName);
		}
		animator.SetFloat(float3Hash, value);
	}

	public void SetAnimatorFloat4(float value)
	{
		if (float4Hash == 0)
		{
			float4Hash = Animator.StringToHash(float4ParameterName);
		}
		animator.SetFloat(float4Hash, value);
	}

	public void SetAnimatorTrigger(string triggerName)
	{
		animator.SetTrigger(triggerName);
	}

	private void Reset()
	{
		animator = GetComponent<Animator>();
	}
}
