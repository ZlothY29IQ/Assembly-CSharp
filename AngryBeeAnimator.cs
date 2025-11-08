using UnityEngine;

public class AngryBeeAnimator : MonoBehaviour
{
	[SerializeField]
	private GameObject beePrefab;

	[SerializeField]
	private int numBees;

	[SerializeField]
	private float orbitMinRadius;

	[SerializeField]
	private float orbitMaxRadius;

	[SerializeField]
	private float orbitMaxHeightDisplacement;

	[SerializeField]
	private float orbitMaxCenterDisplacement;

	[SerializeField]
	private float orbitMaxTilt;

	[SerializeField]
	private float orbitSpeed;

	[SerializeField]
	private float beeScale;

	private GameObject[] beeOrbits;

	private GameObject[] bees;

	private Vector3[] beeOrbitalAxes;

	private float[] beeOrbitalRadii;

	private void Awake()
	{
		bees = new GameObject[numBees];
		beeOrbits = new GameObject[numBees];
		beeOrbitalRadii = new float[numBees];
		beeOrbitalAxes = new Vector3[numBees];
		for (int i = 0; i < numBees; i++)
		{
			GameObject gameObject = new GameObject();
			gameObject.transform.parent = base.transform;
			Vector2 vector = Random.insideUnitCircle * orbitMaxCenterDisplacement;
			gameObject.transform.localPosition = new Vector3(vector.x, Random.Range(0f - orbitMaxHeightDisplacement, orbitMaxHeightDisplacement), vector.y);
			gameObject.transform.localRotation = Quaternion.Euler(Random.Range(0f - orbitMaxTilt, orbitMaxTilt), Random.Range(0, 360), 0f);
			beeOrbitalAxes[i] = gameObject.transform.up;
			GameObject gameObject2 = Object.Instantiate(beePrefab, gameObject.transform);
			float num = Random.Range(orbitMinRadius, orbitMaxRadius);
			beeOrbitalRadii[i] = num;
			gameObject2.transform.localPosition = Vector3.forward * num;
			gameObject2.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
			gameObject2.transform.localScale = Vector3.one * beeScale;
			bees[i] = gameObject2;
			beeOrbits[i] = gameObject;
		}
	}

	private void Update()
	{
		float angle = orbitSpeed * Time.deltaTime;
		for (int i = 0; i < numBees; i++)
		{
			beeOrbits[i].transform.Rotate(beeOrbitalAxes[i], angle);
		}
	}

	public void SetEmergeFraction(float fraction)
	{
		for (int i = 0; i < numBees; i++)
		{
			bees[i].transform.localPosition = Vector3.forward * fraction * beeOrbitalRadii[i];
		}
	}
}
