using UnityEngine;

public class ColliderEnabledManager : MonoBehaviour
{
	public static ColliderEnabledManager instance;

	public Collider[] floorCollider;

	public bool floorEnabled;

	public bool wasFloorEnabled;

	public bool floorCollidersEnabled;

	[GorillaSoundLookup]
	public int wallsBeforeMaterial;

	[GorillaSoundLookup]
	public int wallsAfterMaterial;

	public GorillaSurfaceOverride[] walls;

	public float timeDisabled;

	public float disableLength;

	private void Start()
	{
		floorEnabled = true;
		floorCollidersEnabled = true;
		instance = this;
	}

	private void OnDestroy()
	{
		instance = null;
	}

	public void DisableFloorForFrame()
	{
		floorEnabled = false;
	}

	private void LateUpdate()
	{
		if (!floorEnabled && floorCollidersEnabled)
		{
			DisableFloor();
		}
		if (!floorCollidersEnabled && Time.time > timeDisabled + disableLength)
		{
			floorCollidersEnabled = true;
		}
		Collider[] array = floorCollider;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = floorCollidersEnabled;
		}
		if (floorCollidersEnabled)
		{
			GorillaSurfaceOverride[] array2 = walls;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].overrideIndex = wallsBeforeMaterial;
			}
		}
		else
		{
			GorillaSurfaceOverride[] array2 = walls;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].overrideIndex = wallsAfterMaterial;
			}
		}
		floorEnabled = true;
	}

	private void DisableFloor()
	{
		floorCollidersEnabled = false;
		timeDisabled = Time.time;
	}
}
