using PerformanceSystems;
using UnityEngine;

public class DemoCubeATimeSliceBehaviourEvents : TimeSliceLodBehaviour
{
	[SerializeField]
	private int _iterationsOfExpensiveOp = 200;

	[SerializeField]
	private Material _red;

	[SerializeField]
	private Material _green;

	private Renderer _renderer;

	protected new void Awake()
	{
		base.Awake();
		_renderer = GetComponent<Renderer>();
	}

	public override void SliceUpdate(float deltaTime)
	{
		float num = 0f;
		for (int i = 0; i < _iterationsOfExpensiveOp; i++)
		{
			num += Mathf.Sqrt((float)i * deltaTime);
		}
	}

	public void OnLod0Enter()
	{
		_renderer.material = _red;
		base.gameObject.SetActive(value: true);
	}

	public void OnLod1Enter()
	{
		_renderer.material = _green;
		base.gameObject.SetActive(value: true);
	}

	public void OnLodExit()
	{
		base.gameObject.SetActive(value: false);
	}
}
