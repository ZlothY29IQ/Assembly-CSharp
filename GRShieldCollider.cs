using UnityEngine;

public class GRShieldCollider : MonoBehaviour
{
	[SerializeField]
	private float knockbackVelocity = 3f;

	[SerializeField]
	private GRToolDirectionalShield shieldTool;

	private const float BLOCK_SAME_HITTABLE_COOLDOWN = 1f;

	private GameEntityId lastBlockHittableEntityId;

	private double lastBlockHittableTime;

	public float KnockbackVelocity => knockbackVelocity;

	public GRToolDirectionalShield ShieldTool => shieldTool;

	private void Awake()
	{
		lastBlockHittableEntityId = GameEntityId.Invalid;
		lastBlockHittableTime = 0.0;
	}

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
			double timeAsDouble = Time.timeAsDouble;
			if (!(timeAsDouble - lastBlockHittableTime < 1.0) || !(hittable.gameEntity.id == lastBlockHittableEntityId))
			{
				lastBlockHittableEntityId = hittable.gameEntity.id;
				lastBlockHittableTime = timeAsDouble;
				shieldTool.BlockHittable(enemyPosition, enemyAttackDirection, hittable, this);
			}
		}
	}
}
