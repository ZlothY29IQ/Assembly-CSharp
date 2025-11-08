namespace GTMathUtil;

internal class CriticalSpringDamper
{
	public float x;

	public float xGoal;

	public float halfLife = 0.1f;

	private float curVel;

	private static float halflife_to_damping(float halflife, float eps = 1E-05f)
	{
		return 2.7725887f / (halflife + eps);
	}

	private static float fast_negexp(float x)
	{
		return 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
	}

	public float Update(float dt)
	{
		float num = halflife_to_damping(halfLife) / 2f;
		float num2 = x - xGoal;
		float num3 = curVel + num2 * num;
		float num4 = fast_negexp(num * dt);
		x = num4 * (num2 + num3 * dt) + xGoal;
		curVel = num4 * (curVel - num3 * num * dt);
		return x;
	}
}
