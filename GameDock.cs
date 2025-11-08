using System.Collections.Generic;
using UnityEngine;

public class GameDock : MonoBehaviour
{
	public GameEntity gameEntity;

	public GameDockType dockType;

	public float dockRadius = 0.15f;

	public AbilitySound dockSound;

	public AbilitySound undockSound;

	public AbilityHaptic dockHaptic;

	public Transform dockMarker;

	private List<GameEntity> docked;

	private void Awake()
	{
		docked = new List<GameEntity>(1);
		if (dockMarker == null)
		{
			dockMarker = base.transform;
		}
	}

	private void OnEnable()
	{
	}

	public bool CanDock(GameDockable dockable)
	{
		if (dockable == null)
		{
			return false;
		}
		if (dockType == GameDockType.GRToolDock)
		{
			return GetDockedCount() <= 0;
		}
		return true;
	}

	public int GetDockedCount()
	{
		return docked.Count;
	}

	public void OnDock(GameEntity attachedGameEntity, GameEntity attachedToGameEntity)
	{
		dockSound.Play(null);
		docked.Add(attachedGameEntity);
		dockHaptic.PlayIfSnappedLocal(attachedToGameEntity);
	}

	public void OnUndock(GameEntity gameEntity, GameEntity attachedToGameEntity)
	{
		undockSound.Play(null);
		docked.Remove(gameEntity);
	}
}
