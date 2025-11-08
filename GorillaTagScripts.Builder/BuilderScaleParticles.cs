using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderScaleParticles : MonoBehaviour
{
	private float scale = 1f;

	[Tooltip("Scale particles on enable using lossy scale")]
	[SerializeField]
	private bool useLossyScale;

	[Tooltip("Play particles after scaling")]
	[SerializeField]
	private bool autoPlay;

	[SerializeField]
	private ParticleSystem system;

	[SerializeField]
	private bool scaleShape;

	[SerializeField]
	private bool scaleVelocityLifetime;

	[SerializeField]
	private bool scaleVelocityLimitLifetime;

	[SerializeField]
	private bool scaleForceOverLife;

	private float gravityMod = 1f;

	private ParticleSystem.MinMaxCurve speedCurveCache;

	private ParticleSystem.MinMaxCurve sizeCurveCache;

	private ParticleSystem.MinMaxCurve sizeCurveXCache;

	private ParticleSystem.MinMaxCurve sizeCurveYCache;

	private ParticleSystem.MinMaxCurve sizeCurveZCache;

	private ParticleSystem.MinMaxCurve forceX;

	private ParticleSystem.MinMaxCurve forceY;

	private ParticleSystem.MinMaxCurve forceZ;

	private Vector3 shapeScale = Vector3.one;

	private ParticleSystem.MinMaxCurve lifetimeVelocityX;

	private ParticleSystem.MinMaxCurve lifetimeVelocityY;

	private ParticleSystem.MinMaxCurve lifetimeVelocityZ;

	private float limitMultiplier = 1f;

	private bool shouldRevert;

	private bool setScaleNextFrame;

	private int enableFrame;

	private void OnEnable()
	{
		if (useLossyScale)
		{
			setScaleNextFrame = true;
			enableFrame = Time.frameCount;
		}
	}

	private void LateUpdate()
	{
		if (setScaleNextFrame && Time.frameCount > enableFrame)
		{
			if (useLossyScale)
			{
				SetScale(base.transform.lossyScale.x);
			}
			setScaleNextFrame = false;
		}
	}

	private void OnDisable()
	{
		if (useLossyScale)
		{
			RevertScale();
		}
	}

	public void SetScale(float inScale)
	{
		bool isPlaying = system.isPlaying;
		if (isPlaying)
		{
			system.Stop();
			system.Clear();
		}
		if (Mathf.Approximately(inScale, scale))
		{
			if (autoPlay || isPlaying)
			{
				system.Play(withChildren: true);
			}
			return;
		}
		scale = inScale;
		RevertScale();
		if (Mathf.Approximately(scale, 1f))
		{
			if (autoPlay || isPlaying)
			{
				system.Play(withChildren: true);
			}
			return;
		}
		ParticleSystem.MainModule main = system.main;
		gravityMod = main.gravityModifierMultiplier;
		main.gravityModifierMultiplier = gravityMod * scale;
		if (main.startSize3D)
		{
			ParticleSystem.MinMaxCurve curve = main.startSizeX;
			sizeCurveXCache = main.startSizeX;
			ScaleCurve(ref curve, scale);
			main.startSizeX = curve;
			ParticleSystem.MinMaxCurve curve2 = main.startSizeY;
			sizeCurveYCache = main.startSizeY;
			ScaleCurve(ref curve2, scale);
			main.startSizeY = curve2;
			ParticleSystem.MinMaxCurve curve3 = main.startSizeZ;
			sizeCurveZCache = main.startSizeZ;
			ScaleCurve(ref curve3, scale);
			main.startSizeZ = curve3;
		}
		else
		{
			ParticleSystem.MinMaxCurve curve4 = main.startSize;
			sizeCurveCache = main.startSize;
			ScaleCurve(ref curve4, scale);
			main.startSize = curve4;
		}
		ParticleSystem.MinMaxCurve curve5 = main.startSpeed;
		speedCurveCache = main.startSpeed;
		ScaleCurve(ref curve5, scale);
		main.startSpeed = curve5;
		if (scaleShape)
		{
			ParticleSystem.ShapeModule shape = system.shape;
			shapeScale = shape.scale;
			shape.scale = shapeScale * scale;
		}
		if (scaleVelocityLifetime)
		{
			ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = system.velocityOverLifetime;
			lifetimeVelocityX = velocityOverLifetime.x;
			lifetimeVelocityY = velocityOverLifetime.y;
			lifetimeVelocityZ = velocityOverLifetime.z;
			ParticleSystem.MinMaxCurve curve6 = velocityOverLifetime.x;
			ScaleCurve(ref curve6, scale);
			velocityOverLifetime.x = curve6;
			curve6 = velocityOverLifetime.y;
			ScaleCurve(ref curve6, scale);
			velocityOverLifetime.y = curve6;
			curve6 = velocityOverLifetime.z;
			ScaleCurve(ref curve6, scale);
			velocityOverLifetime.z = curve6;
		}
		if (scaleVelocityLimitLifetime)
		{
			ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityOverLifetime = system.limitVelocityOverLifetime;
			limitMultiplier = limitVelocityOverLifetime.limitMultiplier;
			limitVelocityOverLifetime.limitMultiplier = limitMultiplier * scale;
		}
		if (scaleForceOverLife)
		{
			ParticleSystem.ForceOverLifetimeModule forceOverLifetime = system.forceOverLifetime;
			forceX = forceOverLifetime.x;
			forceY = forceOverLifetime.y;
			forceZ = forceOverLifetime.z;
			ParticleSystem.MinMaxCurve curve7 = forceOverLifetime.x;
			ScaleCurve(ref curve7, scale);
			forceOverLifetime.x = curve7;
			curve7 = forceOverLifetime.y;
			ScaleCurve(ref curve7, scale);
			forceOverLifetime.y = curve7;
			curve7 = forceOverLifetime.z;
			ScaleCurve(ref curve7, scale);
			forceOverLifetime.z = curve7;
		}
		if (autoPlay || isPlaying)
		{
			system.Play(withChildren: true);
		}
		shouldRevert = true;
	}

	private void ScaleCurve(ref ParticleSystem.MinMaxCurve curve, float scale)
	{
		switch (curve.mode)
		{
		case ParticleSystemCurveMode.Constant:
			curve.constant *= scale;
			break;
		case ParticleSystemCurveMode.Curve:
		case ParticleSystemCurveMode.TwoCurves:
			curve.curveMultiplier *= scale;
			break;
		case ParticleSystemCurveMode.TwoConstants:
			curve.constantMin *= scale;
			curve.constantMax *= scale;
			break;
		}
	}

	public void RevertScale()
	{
		if (shouldRevert)
		{
			ParticleSystem.MainModule main = system.main;
			main.gravityModifierMultiplier = gravityMod;
			main.startSpeed = speedCurveCache;
			if (main.startSize3D)
			{
				main.startSizeX = sizeCurveXCache;
				main.startSizeY = sizeCurveYCache;
				main.startSizeZ = sizeCurveZCache;
			}
			else
			{
				main.startSize = sizeCurveCache;
			}
			if (scaleShape)
			{
				ParticleSystem.ShapeModule shape = system.shape;
				shape.scale = shapeScale;
			}
			if (scaleVelocityLifetime)
			{
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = system.velocityOverLifetime;
				velocityOverLifetime.x = lifetimeVelocityX;
				velocityOverLifetime.y = lifetimeVelocityY;
				velocityOverLifetime.z = lifetimeVelocityZ;
			}
			if (scaleVelocityLimitLifetime)
			{
				ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityOverLifetime = system.limitVelocityOverLifetime;
				limitVelocityOverLifetime.limitMultiplier = limitMultiplier;
			}
			if (scaleForceOverLife)
			{
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = system.forceOverLifetime;
				forceOverLifetime.x = forceX;
				forceOverLifetime.y = forceY;
				forceOverLifetime.z = forceZ;
			}
			scale = 1f;
			shouldRevert = false;
		}
	}
}
