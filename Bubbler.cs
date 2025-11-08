using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

public class Bubbler : TransferrableObject
{
	private enum BubblerState
	{
		None = 1,
		Bubbling
	}

	[SerializeField]
	private bool _worksInWater = true;

	public ParticleSystem bubbleParticleSystem;

	private ParticleSystem.Particle[] bubbleParticleArray;

	public AudioSource bubblerAudio;

	public AudioSource popBubbleAudio;

	private List<uint> currentParticles = new List<uint>();

	private Dictionary<uint, Vector3> particleInfoDict = new Dictionary<uint, Vector3>();

	private Vector3 outPosition;

	private bool allBubblesPopped;

	public bool disableActivation;

	public bool disableDeactivation;

	public float rotationSpeed = 5f;

	public GameObject fan;

	public bool fanYaxisinstead;

	public float ongoingStrength = 0.005f;

	public float triggerStrength = 0.2f;

	private float initialTriggerPull;

	private float initialTriggerDuration;

	private bool hasBubblerAudio;

	private bool hasPopBubbleAudio;

	public GameObject gameObjectActiveOnlyWhileTriggerDown;

	public Behaviour[] behavioursToEnableWhenTriggerPressed;

	private bool hasParticleSystem;

	private bool hasFan;

	private bool hasActiveOnlyComponent;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		hasParticleSystem = bubbleParticleSystem != null;
		if (hasParticleSystem)
		{
			bubbleParticleArray = new ParticleSystem.Particle[bubbleParticleSystem.main.maxParticles];
			bubbleParticleSystem.trigger.SetCollider(0, GorillaTagger.Instance.leftHandTriggerCollider.GetComponent<SphereCollider>());
			bubbleParticleSystem.trigger.SetCollider(1, GorillaTagger.Instance.rightHandTriggerCollider.GetComponent<SphereCollider>());
		}
		initialTriggerDuration = 0.05f;
		itemState = ItemStates.State0;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		itemState = ItemStates.State0;
		hasBubblerAudio = bubblerAudio != null && bubblerAudio.clip != null;
		hasPopBubbleAudio = popBubbleAudio != null && popBubbleAudio.clip != null;
		hasFan = fan != null;
		hasActiveOnlyComponent = gameObjectActiveOnlyWhileTriggerDown != null;
	}

	private void InitToDefault()
	{
		itemState = ItemStates.State0;
		if (hasParticleSystem && bubbleParticleSystem.isPlaying)
		{
			bubbleParticleSystem.Stop();
		}
		if (hasBubblerAudio && bubblerAudio.isPlaying)
		{
			bubblerAudio.GTStop();
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		itemState = ItemStates.State0;
		if (hasParticleSystem && bubbleParticleSystem.isPlaying)
		{
			bubbleParticleSystem.Stop();
		}
		if (hasBubblerAudio && bubblerAudio.isPlaying)
		{
			bubblerAudio.GTStop();
		}
		currentParticles.Clear();
		particleInfoDict.Clear();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		InitToDefault();
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (!_worksInWater && GTPlayer.Instance.InWater)
		{
			itemState = ItemStates.State0;
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (!IsMyItem() && base.myOnlineRig != null && base.myOnlineRig.muted)
		{
			itemState = ItemStates.State0;
		}
		bool forLeftController = currentState == PositionState.InLeftHand;
		bool flag = itemState != ItemStates.State0;
		Behaviour[] array = behavioursToEnableWhenTriggerPressed;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = flag;
		}
		if (itemState == ItemStates.State0)
		{
			if (hasParticleSystem && bubbleParticleSystem.isPlaying)
			{
				bubbleParticleSystem.Stop();
			}
			if (hasBubblerAudio && bubblerAudio.isPlaying)
			{
				bubblerAudio.GTStop();
			}
			if (hasActiveOnlyComponent)
			{
				gameObjectActiveOnlyWhileTriggerDown.SetActive(value: false);
			}
		}
		else
		{
			if (hasParticleSystem && !bubbleParticleSystem.isEmitting)
			{
				bubbleParticleSystem.Play();
			}
			if (hasBubblerAudio && !bubblerAudio.isPlaying)
			{
				bubblerAudio.GTPlay();
			}
			if (hasActiveOnlyComponent && !gameObjectActiveOnlyWhileTriggerDown.activeSelf)
			{
				gameObjectActiveOnlyWhileTriggerDown.SetActive(value: true);
			}
			if (IsMyItem())
			{
				initialTriggerPull = Time.time;
				GorillaTagger.Instance.StartVibration(forLeftController, triggerStrength, initialTriggerDuration);
				if (Time.time > initialTriggerPull + initialTriggerDuration)
				{
					GorillaTagger.Instance.StartVibration(forLeftController, ongoingStrength, Time.deltaTime);
				}
			}
			if (hasFan)
			{
				if (!fanYaxisinstead)
				{
					float z = fan.transform.localEulerAngles.z + rotationSpeed * Time.fixedDeltaTime;
					fan.transform.localEulerAngles = new Vector3(0f, 0f, z);
				}
				else
				{
					float y = fan.transform.localEulerAngles.y + rotationSpeed * Time.fixedDeltaTime;
					fan.transform.localEulerAngles = new Vector3(0f, y, 0f);
				}
			}
		}
		if (!hasParticleSystem || (allBubblesPopped && itemState != ItemStates.State1))
		{
			return;
		}
		int particles = bubbleParticleSystem.GetParticles(bubbleParticleArray);
		allBubblesPopped = particles <= 0;
		if (allBubblesPopped)
		{
			return;
		}
		for (int j = 0; j < particles; j++)
		{
			if (currentParticles.Contains(bubbleParticleArray[j].randomSeed))
			{
				currentParticles.Remove(bubbleParticleArray[j].randomSeed);
			}
		}
		foreach (uint currentParticle in currentParticles)
		{
			if (particleInfoDict.TryGetValue(currentParticle, out outPosition))
			{
				if (hasPopBubbleAudio)
				{
					GTAudioSourceExtensions.GTPlayClipAtPoint(popBubbleAudio.clip, outPosition);
				}
				particleInfoDict.Remove(currentParticle);
			}
		}
		currentParticles.Clear();
		for (int k = 0; k < particles; k++)
		{
			if (particleInfoDict.TryGetValue(bubbleParticleArray[k].randomSeed, out outPosition))
			{
				particleInfoDict[bubbleParticleArray[k].randomSeed] = bubbleParticleArray[k].position;
			}
			else
			{
				particleInfoDict.Add(bubbleParticleArray[k].randomSeed, bubbleParticleArray[k].position);
			}
			currentParticles.Add(bubbleParticleArray[k].randomSeed);
		}
	}

	public override void OnActivate()
	{
		base.OnActivate();
		itemState = ItemStates.State1;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		itemState = ItemStates.State0;
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
