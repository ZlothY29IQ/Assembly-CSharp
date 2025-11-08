using UnityEngine;

public class CrittersFoodSettings : CrittersActorSettings
{
	public float _maxFood;

	public float _currentFood;

	public float _startingSize;

	public float _currentSize;

	public Transform _food;

	public bool _disableWhenEmpty;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersFood obj = (CrittersFood)parentActor;
		obj.maxFood = _maxFood;
		obj.currentFood = _currentFood;
		obj.startingSize = _startingSize;
		obj.currentSize = _currentSize;
		obj.food = _food;
		obj.disableWhenEmpty = _disableWhenEmpty;
		obj.SpawnData(_maxFood, _currentFood, _startingSize);
	}
}
