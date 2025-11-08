using System;
using GorillaExtensions;
using GorillaTag;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UseableObjectEvents))]
public class UseableObject : TransferrableObject
{
	[DebugOption]
	public bool disableActivation;

	[DebugOption]
	public bool disableDeactivation;

	[SerializeField]
	private UseableObjectEvents _events;

	[SerializeField]
	private bool _raiseActivate = true;

	[SerializeField]
	private bool _raiseDeactivate = true;

	[NonSerialized]
	private DateTime _lastActivate;

	[NonSerialized]
	private DateTime _lastDeactivate;

	[NonSerialized]
	private bool _isMidUse;

	[NonSerialized]
	private float _useTimeElapsed;

	[NonSerialized]
	private bool _justUsed;

	[NonSerialized]
	private int tempHandPos;

	public UnityEvent onActivateLocal;

	public UnityEvent onDeactivateLocal;

	public bool isMidUse => _isMidUse;

	public float useTimeElapsed => _useTimeElapsed;

	public bool justUsed
	{
		get
		{
			if (!_justUsed)
			{
				return false;
			}
			_justUsed = false;
			return true;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		_events = base.gameObject.GetOrAddComponent<UseableObjectEvents>();
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		_events.Init(base.myOnlineRig?.creator ?? base.myRig?.creator);
		_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnObjectActivated);
		_events.Deactivate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnObjectDeactivated);
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		UnityEngine.Object.Destroy(_events);
	}

	private void OnObjectActivated(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
	}

	private void OnObjectDeactivated(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
	}

	public override void TriggeredLateUpdate()
	{
		base.TriggeredLateUpdate();
		if (_isMidUse)
		{
			_useTimeElapsed += Time.deltaTime;
		}
	}

	public override void OnActivate()
	{
		base.OnActivate();
		if (IsMyItem())
		{
			onActivateLocal?.Invoke();
			_useTimeElapsed = 0f;
			_isMidUse = true;
		}
		if (_raiseActivate)
		{
			_events?.Activate?.RaiseAll();
		}
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		if (IsMyItem())
		{
			onDeactivateLocal?.Invoke();
			_isMidUse = false;
			_justUsed = true;
		}
		if (_raiseDeactivate)
		{
			_events?.Deactivate?.RaiseAll();
		}
	}

	public override bool CanActivate()
	{
		return !disableActivation;
	}

	public override bool CanDeactivate()
	{
		return !disableDeactivation;
	}
}
