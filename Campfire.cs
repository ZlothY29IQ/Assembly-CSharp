using UnityEngine;

public class Campfire : MonoBehaviour, IGorillaSliceableSimple
{
	public Transform baseFire;

	public Transform middleFire;

	public Transform topFire;

	public float baseMultiplier;

	public float middleMultiplier;

	public float topMultiplier;

	public float bottomRange;

	public float middleRange;

	public float topRange;

	private float lastAngleBottom;

	private float lastAngleMiddle;

	private float lastAngleTop;

	public float perlinStepBottom;

	public float perlinStepMiddle;

	public float perlinStepTop;

	private float perlinBottom;

	private float perlinMiddle;

	private float perlinTop;

	public float startingRotationBottom;

	public float startingRotationMiddle;

	public float startingRotationTop;

	public float slerp = 0.01f;

	private bool mergedBottom;

	private bool mergedMiddle;

	private bool mergedTop;

	public string lastTimeOfDay;

	public Material mat;

	private float h;

	private float s;

	private float v;

	public int overrideDayNight;

	private Vector3 tempVec;

	public bool[] isActive;

	public bool wasActive;

	private float lastTime;

	public bool playDuringRain;

	private void Start()
	{
		lastAngleBottom = 0f;
		lastAngleMiddle = 0f;
		lastAngleTop = 0f;
		perlinBottom = Random.Range(0, 100);
		perlinMiddle = Random.Range(200, 300);
		perlinTop = Random.Range(400, 500);
		startingRotationBottom = baseFire.localEulerAngles.x;
		startingRotationMiddle = middleFire.localEulerAngles.x;
		startingRotationTop = topFire.localEulerAngles.x;
		tempVec = new Vector3(0f, 0f, 0f);
		mergedBottom = false;
		mergedMiddle = false;
		mergedTop = false;
		wasActive = false;
		lastTime = Time.time;
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		if (BetterDayNightManager.instance == null)
		{
			return;
		}
		if ((isActive[BetterDayNightManager.instance.currentTimeIndex] && (playDuringRain || BetterDayNightManager.instance.CurrentWeather() != BetterDayNightManager.WeatherType.Raining)) || overrideDayNight == 1)
		{
			if (!wasActive)
			{
				wasActive = true;
				mergedBottom = false;
				mergedMiddle = false;
				mergedTop = false;
				Color.RGBToHSV(mat.color, out h, out s, out v);
				mat.color = Color.HSVToRGB(h, s, 1f);
			}
			Flap(ref perlinBottom, perlinStepBottom, ref lastAngleBottom, ref baseFire, bottomRange, baseMultiplier, ref mergedBottom);
			Flap(ref perlinMiddle, perlinStepMiddle, ref lastAngleMiddle, ref middleFire, middleRange, middleMultiplier, ref mergedMiddle);
			Flap(ref perlinTop, perlinStepTop, ref lastAngleTop, ref topFire, topRange, topMultiplier, ref mergedTop);
		}
		else
		{
			if (wasActive)
			{
				wasActive = false;
				mergedBottom = false;
				mergedMiddle = false;
				mergedTop = false;
				Color.RGBToHSV(mat.color, out h, out s, out v);
				mat.color = Color.HSVToRGB(h, s, 0.25f);
			}
			ReturnToOff(ref baseFire, startingRotationBottom, ref mergedBottom);
			ReturnToOff(ref middleFire, startingRotationMiddle, ref mergedMiddle);
			ReturnToOff(ref topFire, startingRotationTop, ref mergedTop);
		}
		lastTime = Time.time;
	}

	private void Flap(ref float perlinValue, float perlinStep, ref float lastAngle, ref Transform flameTransform, float range, float multiplier, ref bool isMerged)
	{
		perlinValue += perlinStep;
		lastAngle += (Time.time - lastTime) * Mathf.PerlinNoise(perlinValue, 0f);
		tempVec.x = range * Mathf.Sin(lastAngle * multiplier);
		if (Mathf.Abs(tempVec.x - flameTransform.localEulerAngles.x) > 180f)
		{
			if (tempVec.x > flameTransform.localEulerAngles.x)
			{
				tempVec.x -= 360f;
			}
			else
			{
				tempVec.x += 360f;
			}
		}
		if (isMerged)
		{
			flameTransform.localEulerAngles = tempVec;
		}
		else if (Mathf.Abs(flameTransform.localEulerAngles.x - tempVec.x) < 1f)
		{
			isMerged = true;
			flameTransform.localEulerAngles = tempVec;
		}
		else
		{
			tempVec.x = (tempVec.x - flameTransform.localEulerAngles.x) * slerp + flameTransform.localEulerAngles.x;
			flameTransform.localEulerAngles = tempVec;
		}
	}

	private void ReturnToOff(ref Transform startTransform, float targetAngle, ref bool isMerged)
	{
		tempVec.x = targetAngle;
		if (Mathf.Abs(tempVec.x - startTransform.localEulerAngles.x) > 180f)
		{
			if (tempVec.x > startTransform.localEulerAngles.x)
			{
				tempVec.x -= 360f;
			}
			else
			{
				tempVec.x += 360f;
			}
		}
		if (!isMerged)
		{
			if (Mathf.Abs(startTransform.localEulerAngles.x - targetAngle) < 1f)
			{
				isMerged = true;
				return;
			}
			tempVec.x = (tempVec.x - startTransform.localEulerAngles.x) * slerp + startTransform.localEulerAngles.x;
			startTransform.localEulerAngles = tempVec;
		}
	}
}
