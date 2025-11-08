using UnityEngine;

public class GRShieldCollider : MonoBehaviour
{
	[SerializeField]
	private float knockbackVelocity = 3f;

	[SerializeField]
	private GRToolDirectionalShield shieldTool;

	public float KnockbackVelocity => knockbackVelocity;

	public GRToolDirectionalShield ShieldTool => shieldTool;

	public void OnEnemyBlocked(Vector3 enemyPosition)
	{
		if (shieldTool != null)
		{
			shieldTool.OnEnemyBlocked(enemyPosition);
		}
	}

	public void BlockHittable(Vector3 enemyPosition, Vector3 enemyAttackDirection, GameHittable hittable)
	{
		if (shieldTool != null)
		{
			shieldTool.BlockHittable(enemyPosition, enemyAttackDirection, hittable, this);
		}
	}
}
