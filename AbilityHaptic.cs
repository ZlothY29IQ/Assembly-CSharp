using System;

[Serializable]
public class AbilityHaptic
{
	public float strength = 0.2f;

	public float duration = 0.1f;

	public void PlayIfHeldLocal(GameEntity gameEntity)
	{
		if (gameEntity == null || !gameEntity.IsHeldByLocalPlayer())
		{
			return;
		}
		GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer);
		if (!(out_gamePlayer == null))
		{
			int num = out_gamePlayer.FindHandIndex(gameEntity.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}

	public void PlayIfSnappedLocal(GameEntity gameEntity)
	{
		if (gameEntity == null || !gameEntity.IsSnappedByLocalPlayer())
		{
			return;
		}
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component == null)
		{
			return;
		}
		if (component.IsSnappedToLeftArm())
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, strength, duration);
		}
		if (component.IsSnappedToRightArm())
		{
			GorillaTagger.Instance.StartVibration(forLeftController: false, strength, duration);
		}
		GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer);
		if (!(out_gamePlayer == null))
		{
			int num = out_gamePlayer.FindHandIndex(gameEntity.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}
}
