using UnityEngine;

public class CosmeticCritterButterfly : CosmeticCritter
{
	[Tooltip("The speed this Butterfly will move at.")]
	[SerializeField]
	private float speed = 1f;

	[Tooltip("Emit one particle from this particle system when spawning.")]
	[SerializeField]
	private ParticleSystem particleSystem;

	private Vector3 startPosition;

	private Vector3 direction;

	private ParticleSystem.EmitParams emitParams;

	public ParticleSystem.EmitParams GetEmitParams => emitParams;

	public void SetStartPos(Vector3 initialPos)
	{
		startPosition = initialPos;
	}

	public override void SetRandomVariables()
	{
		direction = Random.insideUnitSphere;
		emitParams.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
		particleSystem.Emit(emitParams, 1);
	}

	public override void Tick()
	{
		base.transform.position = startPosition + (float)GetAliveTime() * speed * direction;
	}
}
