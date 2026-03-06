using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Fusion;
using GorillaLocomotion;
using GorillaLocomotion.Gameplay;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

[NetworkBehaviourWeaved(4)]
public class MagicCauldron : NetworkComponent
{
	private enum CauldronState
	{
		notReady,
		ready,
		recipeCollecting,
		recipeActivated,
		summoned,
		failed,
		cooldown
	}

	[Serializable]
	public struct Recipe
	{
		public List<MagicIngredientType> recipeIngredients;

		public AudioClip successAudio;
	}

	private class IngredientArgs : FXSArgs
	{
		public int key;
	}

	private class IngrediantFXContext : IFXContextParems<IngredientArgs>
	{
		public delegate void Callback(int key);

		public FXSystemSettings playerSettings;

		public Callback fxCallBack;

		FXSystemSettings IFXContextParems<IngredientArgs>.settings => playerSettings;

		void IFXContextParems<IngredientArgs>.OnPlayFX(IngredientArgs args)
		{
			fxCallBack(args.key);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 16)]
	[NetworkStructWeaved(4)]
	private struct MagicCauldronData : INetworkStruct
	{
		[field: FieldOffset(0)]
		public float CurrentStateElapsedTime { get; set; }

		[field: FieldOffset(4)]
		public int CurrentRecipeIndex { get; set; }

		[field: FieldOffset(8)]
		public CauldronState CurrentState { get; set; }

		[field: FieldOffset(12)]
		public int IngredientIndex { get; set; }

		public MagicCauldronData(float stateElapsedTime, int recipeIndex, CauldronState state, int ingredientIndex)
		{
			CurrentStateElapsedTime = stateElapsedTime;
			CurrentRecipeIndex = recipeIndex;
			CurrentState = state;
			IngredientIndex = ingredientIndex;
		}
	}

	public List<Recipe> recipes = new List<Recipe>();

	public float maxTimeToAddAllIngredients = 30f;

	public float summonWitchesDuration = 20f;

	public float recipeFailedDuration = 5f;

	public float cooldownDuration = 30f;

	public MagicIngredientType[] allIngredients;

	public GameObject flyingWitchesContainer;

	[SerializeField]
	private AudioSource audioSource;

	public AudioClip ingredientAddedAudio;

	public AudioClip recipeFailedAudio;

	public ParticleSystem bubblesParticle;

	public ParticleSystem successParticle;

	public ParticleSystem splashParticle;

	public Color CauldronActiveColor;

	public Color CauldronFailedColor;

	[Tooltip("only if we are using the time of day event")]
	public Color CauldronNotReadyColor;

	private readonly List<NoncontrollableBroomstick> witchesComponent = new List<NoncontrollableBroomstick>();

	private readonly List<MagicIngredientType> currentIngredients = new List<MagicIngredientType>();

	private float currentStateElapsedTime;

	private CauldronState currentState;

	[SerializeField]
	private Renderer rendr;

	private Color cauldronColor;

	private Color currentColor;

	private int currentRecipeIndex;

	private int ingredientIndex;

	private float waitTimeToSummonWitches = 2f;

	[Space]
	[SerializeField]
	private MagicCauldronLiquid _liquid;

	private IngrediantFXContext reusableFXContext = new IngrediantFXContext();

	private IngredientArgs reusableIngrediantArgs = new IngredientArgs();

	public bool testLevitationAlwaysOn;

	public float levitationRadius;

	public float levitationSpellDuration;

	public float levitationStrength;

	public float levitationDuration;

	public float levitationBlendOutDuration;

	public float levitationBonusStrength;

	public float levitationBonusOffAtYSpeed;

	public float levitationBonusFullAtYSpeed;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 4)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private MagicCauldronData _Data;

	[Networked]
	[NetworkedWeaved(0, 4)]
	private unsafe MagicCauldronData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing MagicCauldron.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(MagicCauldronData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing MagicCauldron.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(MagicCauldronData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	private new void Awake()
	{
		currentIngredients.Clear();
		witchesComponent.Clear();
		currentStateElapsedTime = 0f;
		currentRecipeIndex = -1;
		ingredientIndex = -1;
		if (flyingWitchesContainer != null)
		{
			for (int i = 0; i < flyingWitchesContainer.transform.childCount; i++)
			{
				NoncontrollableBroomstick componentInChildren = flyingWitchesContainer.transform.GetChild(i).gameObject.GetComponentInChildren<NoncontrollableBroomstick>();
				witchesComponent.Add(componentInChildren);
				if ((bool)componentInChildren)
				{
					componentInChildren.gameObject.SetActive(value: false);
				}
			}
		}
		if (reusableFXContext == null)
		{
			reusableFXContext = new IngrediantFXContext();
		}
		if (reusableIngrediantArgs == null)
		{
			reusableIngrediantArgs = new IngredientArgs();
		}
		reusableFXContext.fxCallBack = OnIngredientAdd;
	}

	private new void Start()
	{
		ChangeState(CauldronState.notReady);
	}

	private void LateUpdate()
	{
		UpdateState();
	}

	private IEnumerator LevitationSpellCoroutine()
	{
		GTPlayer.Instance.SetHalloweenLevitation(levitationStrength, levitationDuration, levitationBlendOutDuration, levitationBonusStrength, levitationBonusOffAtYSpeed, levitationBonusFullAtYSpeed);
		yield return new WaitForSeconds(levitationSpellDuration);
		GTPlayer.Instance.SetHalloweenLevitation(0f, levitationDuration, levitationBlendOutDuration, 0f, levitationBonusOffAtYSpeed, levitationBonusFullAtYSpeed);
	}

	private void ChangeState(CauldronState state)
	{
		currentState = state;
		if (base.IsMine)
		{
			currentStateElapsedTime = 0f;
		}
		bool flag = state == CauldronState.summoned;
		foreach (NoncontrollableBroomstick item in witchesComponent)
		{
			if (item.gameObject.activeSelf != flag)
			{
				item.gameObject.SetActive(flag);
			}
		}
		if (currentState == CauldronState.summoned && Vector3.Distance(GTPlayer.Instance.transform.position, base.transform.position) < levitationRadius)
		{
			StartCoroutine(LevitationSpellCoroutine());
		}
		switch (currentState)
		{
		case CauldronState.notReady:
			currentIngredients.Clear();
			UpdateCauldronColor(CauldronNotReadyColor);
			break;
		case CauldronState.ready:
			UpdateCauldronColor(CauldronActiveColor);
			break;
		case CauldronState.recipeCollecting:
			if (ingredientIndex >= 0 && ingredientIndex < allIngredients.Length)
			{
				UpdateCauldronColor(allIngredients[ingredientIndex].color);
			}
			break;
		case CauldronState.recipeActivated:
			if ((bool)audioSource && (bool)recipes[currentRecipeIndex].successAudio)
			{
				audioSource.GTPlayOneShot(recipes[currentRecipeIndex].successAudio);
			}
			if ((bool)successParticle)
			{
				successParticle.Play();
			}
			break;
		case CauldronState.failed:
			currentIngredients.Clear();
			UpdateCauldronColor(CauldronFailedColor);
			audioSource.GTPlayOneShot(recipeFailedAudio);
			break;
		case CauldronState.cooldown:
			currentIngredients.Clear();
			UpdateCauldronColor(CauldronFailedColor);
			break;
		case CauldronState.summoned:
			break;
		}
	}

	private void UpdateState()
	{
		if (!base.IsMine)
		{
			return;
		}
		currentStateElapsedTime += Time.deltaTime;
		switch (currentState)
		{
		case CauldronState.recipeCollecting:
			if (currentStateElapsedTime >= maxTimeToAddAllIngredients && !CheckIngredients())
			{
				ChangeState(CauldronState.failed);
			}
			break;
		case CauldronState.recipeActivated:
			if (currentStateElapsedTime >= waitTimeToSummonWitches)
			{
				ChangeState(CauldronState.summoned);
			}
			break;
		case CauldronState.summoned:
			if (currentStateElapsedTime >= summonWitchesDuration)
			{
				ChangeState(CauldronState.cooldown);
			}
			break;
		case CauldronState.failed:
			if (currentStateElapsedTime >= recipeFailedDuration)
			{
				ChangeState(CauldronState.ready);
			}
			break;
		case CauldronState.cooldown:
			if (currentStateElapsedTime >= cooldownDuration)
			{
				ChangeState(CauldronState.ready);
			}
			break;
		case CauldronState.notReady:
		case CauldronState.ready:
			break;
		}
	}

	public void OnEventStart()
	{
		ChangeState(CauldronState.ready);
	}

	public void OnEventEnd()
	{
		ChangeState(CauldronState.notReady);
	}

	[PunRPC]
	public void OnIngredientAdd(int _ingredientIndex, PhotonMessageInfo info)
	{
		OnIngredientAddShared(_ingredientIndex, info);
	}

	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	public unsafe void RPC_OnIngredientAdd(int _ingredientIndex, RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 1) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void MagicCauldron::RPC_OnIngredientAdd(System.Int32,Fusion.RpcInfo)", base.Object, 1);
				return;
			}
			int num = 8;
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void MagicCauldron::RPC_OnIngredientAdd(System.Int32,Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
				int num2 = 8;
				*(int*)(ptr2 + num2) = _ingredientIndex;
				num2 += 4;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		OnIngredientAddShared(_ingredientIndex, info);
	}

	private void OnIngredientAddShared(int _ingredientIndex, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "OnIngredientAdd");
		if (VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			reusableFXContext.playerSettings = playerRig.Rig.fxSettings;
			reusableIngrediantArgs.key = _ingredientIndex;
			FXSystem.PlayFX(FXType.HWIngredients, reusableFXContext, reusableIngrediantArgs, info);
		}
	}

	private void OnIngredientAdd(int _ingredientIndex)
	{
		if ((bool)audioSource)
		{
			audioSource.GTPlayOneShot(ingredientAddedAudio);
		}
		if (!RoomSystem.AmITheHost || _ingredientIndex < 0 || _ingredientIndex >= allIngredients.Length || (currentState != CauldronState.ready && currentState != CauldronState.recipeCollecting))
		{
			return;
		}
		MagicIngredientType magicIngredientType = allIngredients[_ingredientIndex];
		Debug.Log($"Received ingredient RPC {_ingredientIndex} = {magicIngredientType}");
		MagicIngredientType magicIngredientType2 = null;
		if (recipes[0].recipeIngredients.Count > currentIngredients.Count)
		{
			magicIngredientType2 = recipes[0].recipeIngredients[currentIngredients.Count];
		}
		if (magicIngredientType == magicIngredientType2)
		{
			ingredientIndex = _ingredientIndex;
			currentIngredients.Add(magicIngredientType);
			if (CheckIngredients())
			{
				ChangeState(CauldronState.recipeActivated);
			}
			else if (currentState == CauldronState.ready)
			{
				ChangeState(CauldronState.recipeCollecting);
			}
			else
			{
				UpdateCauldronColor(magicIngredientType.color);
			}
		}
		else
		{
			Debug.Log($"Failure: Expected ingredient {magicIngredientType2}, got {magicIngredientType} from recipe[{currentIngredients.Count}]");
			ChangeState(CauldronState.failed);
		}
	}

	private bool CheckIngredients()
	{
		foreach (Recipe recipe in recipes)
		{
			if (currentIngredients.SequenceEqual(recipe.recipeIngredients))
			{
				currentRecipeIndex = recipes.IndexOf(recipe);
				return true;
			}
		}
		return false;
	}

	private void UpdateCauldronColor(Color color)
	{
		if ((bool)bubblesParticle)
		{
			if (bubblesParticle.isPlaying)
			{
				if (currentState == CauldronState.failed || currentState == CauldronState.notReady)
				{
					bubblesParticle.Stop();
				}
			}
			else
			{
				bubblesParticle.Play();
			}
		}
		currentColor = cauldronColor;
		if (!(currentColor == color))
		{
			if ((bool)rendr)
			{
				_liquid.AnimateColorFromTo(cauldronColor, color);
				cauldronColor = color;
			}
			if ((bool)bubblesParticle)
			{
				ParticleSystem.MainModule main = bubblesParticle.main;
				main.startColor = color;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		ThrowableSetDressing componentInParent = other.GetComponentInParent<ThrowableSetDressing>();
		if (componentInParent == null || componentInParent.IngredientTypeSO == null || componentInParent.InHand())
		{
			return;
		}
		if (componentInParent.IsLocalOwnedWorldShareable)
		{
			if (componentInParent.IngredientTypeSO != null && (currentState == CauldronState.ready || currentState == CauldronState.recipeCollecting))
			{
				int num = allIngredients.IndexOfRef(componentInParent.IngredientTypeSO);
				Debug.Log($"Sending ingredient RPC {componentInParent.IngredientTypeSO} = {num}");
				SendRPC("OnIngredientAdd", RpcTarget.Others, num);
				OnIngredientAdd(num);
			}
			componentInParent.StartRespawnTimer(0f);
		}
		if (componentInParent.IngredientTypeSO != null && (bool)splashParticle)
		{
			splashParticle.Play();
		}
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		currentIngredients.Clear();
	}

	public override void WriteDataFusion()
	{
		Data = new MagicCauldronData(currentStateElapsedTime, currentRecipeIndex, currentState, ingredientIndex);
	}

	public override void ReadDataFusion()
	{
		ReadDataShared(Data.CurrentStateElapsedTime, Data.CurrentRecipeIndex, Data.CurrentState, Data.IngredientIndex);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			stream.SendNext(currentStateElapsedTime);
			stream.SendNext(currentRecipeIndex);
			stream.SendNext(currentState);
			stream.SendNext(ingredientIndex);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			float stateElapsedTime = (float)stream.ReceiveNext();
			int recipeIndex = (int)stream.ReceiveNext();
			CauldronState state = (CauldronState)stream.ReceiveNext();
			int num = (int)stream.ReceiveNext();
			ReadDataShared(stateElapsedTime, recipeIndex, state, num);
		}
	}

	private void ReadDataShared(float stateElapsedTime, int recipeIndex, CauldronState state, int ingredientIndex)
	{
		CauldronState num = currentState;
		currentStateElapsedTime = stateElapsedTime;
		currentRecipeIndex = recipeIndex;
		currentState = state;
		this.ingredientIndex = ingredientIndex;
		if (num != currentState)
		{
			ChangeState(currentState);
		}
		else if (currentState == CauldronState.recipeCollecting && ingredientIndex != ingredientIndex && ingredientIndex >= 0 && ingredientIndex < allIngredients.Length)
		{
			UpdateCauldronColor(allIngredients[ingredientIndex].color);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}

	[NetworkRpcWeavedInvoker(1, 1, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_OnIngredientAdd_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int num3 = num2;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((MagicCauldron)behaviour).RPC_OnIngredientAdd(num3, info);
	}
}
