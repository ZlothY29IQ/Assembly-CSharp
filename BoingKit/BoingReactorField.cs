using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoingKit;

public class BoingReactorField : BoingBase
{
	public enum HardwareModeEnum
	{
		CPU,
		GPU
	}

	public enum CellMoveModeEnum
	{
		Follow,
		WrapAround
	}

	public enum FalloffModeEnum
	{
		None,
		Circle,
		Square
	}

	public enum FalloffDimensionsEnum
	{
		XYZ,
		XY,
		XZ,
		YZ
	}

	public class ShaderPropertyIdSet
	{
		public int MoveParams;

		public int WrapParams;

		public int Effectors;

		public int EffectorIndices;

		public int ReactorParams;

		public int ComputeFieldParams;

		public int ComputeCells;

		public int RenderFieldParams;

		public int RenderCells;

		public int PositionSampleMultiplier;

		public int RotationSampleMultiplier;

		public int PropagationParams;

		public ShaderPropertyIdSet()
		{
			MoveParams = Shader.PropertyToID("moveParams");
			WrapParams = Shader.PropertyToID("wrapParams");
			Effectors = Shader.PropertyToID("aEffector");
			EffectorIndices = Shader.PropertyToID("aEffectorIndex");
			ReactorParams = Shader.PropertyToID("reactorParams");
			ComputeFieldParams = Shader.PropertyToID("fieldParams");
			ComputeCells = Shader.PropertyToID("aCell");
			RenderFieldParams = Shader.PropertyToID("aBoingFieldParams");
			RenderCells = Shader.PropertyToID("aBoingFieldCell");
			PositionSampleMultiplier = Shader.PropertyToID("positionSampleMultiplier");
			RotationSampleMultiplier = Shader.PropertyToID("rotationSampleMultiplier");
			PropagationParams = Shader.PropertyToID("propagationParams");
		}
	}

	private struct FieldParams
	{
		public static readonly int Stride = 112;

		public int CellsX;

		public int CellsY;

		public int CellsZ;

		public int NumEffectors;

		public int iCellBaseX;

		public int iCellBaseY;

		public int iCellBaseZ;

		public int m_padding0;

		public int FalloffMode;

		public int FalloffDimensions;

		public int PropagationDepth;

		public int m_padding1;

		public Vector3 GridCenter;

		private float m_padding3;

		public Vector3 UpWs;

		private float m_padding2;

		public Vector3 FieldPosition;

		public float m_padding4;

		public float FalloffRatio;

		public float CellSize;

		public float DeltaTime;

		private float m_padding5;

		private void SuppressWarnings()
		{
			m_padding0 = 0;
			m_padding1 = 0;
			m_padding2 = 0f;
			m_padding4 = 0f;
			m_padding5 = 0f;
			m_padding0 = m_padding1;
			m_padding1 = (int)m_padding2;
			m_padding2 = m_padding3;
			m_padding3 = m_padding4;
			m_padding4 = m_padding5;
		}
	}

	private class ComputeKernelId
	{
		public int InitKernel;

		public int MoveKernel;

		public int WrapXKernel;

		public int WrapYKernel;

		public int WrapZKernel;

		public int ExecuteKernel;
	}

	private static ShaderPropertyIdSet s_shaderPropertyId;

	private FieldParams m_fieldParams;

	public HardwareModeEnum HardwareMode = HardwareModeEnum.GPU;

	private HardwareModeEnum m_hardwareMode;

	public CellMoveModeEnum CellMoveMode = CellMoveModeEnum.WrapAround;

	private CellMoveModeEnum m_cellMoveMode;

	[Range(0.1f, 10f)]
	public float CellSize = 1f;

	public int CellsX = 8;

	public int CellsY = 1;

	public int CellsZ = 8;

	private int m_cellsX = -1;

	private int m_cellsY = -1;

	private int m_cellsZ = -1;

	private int m_iCellBaseX;

	private int m_iCellBaseY;

	private int m_iCellBaseZ;

	public FalloffModeEnum FalloffMode = FalloffModeEnum.Square;

	[Range(0f, 1f)]
	public float FalloffRatio = 0.7f;

	public FalloffDimensionsEnum FalloffDimensions = FalloffDimensionsEnum.XZ;

	public BoingEffector[] Effectors = new BoingEffector[1];

	private int m_numEffectors = -1;

	private Aabb m_bounds;

	public bool TwoDDistanceCheck;

	public bool TwoDPositionInfluence;

	public bool TwoDRotationInfluence;

	public bool EnablePositionEffect = true;

	public bool EnableRotationEffect = true;

	public bool GlobalReactionUpVector;

	public BoingWork.Params Params;

	public SharedBoingParams SharedParams;

	public bool EnablePropagation;

	[Range(0f, 1f)]
	public float PositionPropagation = 1f;

	[Range(0f, 1f)]
	public float RotationPropagation = 1f;

	[Range(1f, 3f)]
	public int PropagationDepth = 1;

	public bool AnchorPropagationAtBorder;

	private static readonly float kPropagationFactor = 600f;

	private BoingWork.Params.InstanceData[,,] m_aCpuCell;

	private ComputeShader m_shader;

	private ComputeBuffer m_effectorIndexBuffer;

	private ComputeBuffer m_reactorParamsBuffer;

	private ComputeBuffer m_fieldParamsBuffer;

	private ComputeBuffer m_cellsBuffer;

	private int m_gpuResourceSetId = -1;

	private static ComputeKernelId s_computeKernelId;

	private bool m_init;

	private Vector3 m_gridCenter;

	private Vector3 m_qPrevGridCenterNorm;

	private static Vector3[] s_aCellOffset = new Vector3[8];

	private bool m_cellBufferNeedsReset;

	private static float[] s_aSqrtInv = new float[28]
	{
		0f,
		1f,
		0.70711f,
		0.57735f,
		0.5f,
		0.44721f,
		0.40825f,
		0.37796f,
		0.35355f,
		0.33333f,
		0.31623f,
		0.30151f,
		0.28868f,
		0.27735f,
		0.26726f,
		0.2582f,
		0.25f,
		149f / (226f * MathF.E),
		0.2357f,
		0.22942f,
		0.22361f,
		0.21822f,
		0.2132f,
		0.20851f,
		0.20412f,
		0.2f,
		0.19612f,
		0.19245f
	};

	private BoingWork.Params[] s_aReactorParams = new BoingWork.Params[1];

	public static ShaderPropertyIdSet ShaderPropertyId
	{
		get
		{
			if (s_shaderPropertyId == null)
			{
				s_shaderPropertyId = new ShaderPropertyIdSet();
			}
			return s_shaderPropertyId;
		}
	}

	public int GpuResourceSetId => m_gpuResourceSetId;

	public bool UpdateShaderConstants(MaterialPropertyBlock props, float positionSampleMultiplier = 1f, float rotationSampleMultiplier = 1f)
	{
		if (HardwareMode != HardwareModeEnum.GPU)
		{
			return false;
		}
		if (m_fieldParamsBuffer == null || m_cellsBuffer == null)
		{
			return false;
		}
		props.SetFloat(ShaderPropertyId.PositionSampleMultiplier, positionSampleMultiplier);
		props.SetFloat(ShaderPropertyId.RotationSampleMultiplier, rotationSampleMultiplier);
		props.SetBuffer(ShaderPropertyId.RenderFieldParams, m_fieldParamsBuffer);
		props.SetBuffer(ShaderPropertyId.RenderCells, m_cellsBuffer);
		return true;
	}

	public bool UpdateShaderConstants(Material material, float positionSampleMultiplier = 1f, float rotationSampleMultiplier = 1f)
	{
		if (HardwareMode != HardwareModeEnum.GPU)
		{
			return false;
		}
		if (m_fieldParamsBuffer == null || m_cellsBuffer == null)
		{
			return false;
		}
		material.SetFloat(ShaderPropertyId.PositionSampleMultiplier, positionSampleMultiplier);
		material.SetFloat(ShaderPropertyId.RotationSampleMultiplier, rotationSampleMultiplier);
		material.SetBuffer(ShaderPropertyId.RenderFieldParams, m_fieldParamsBuffer);
		material.SetBuffer(ShaderPropertyId.RenderCells, m_cellsBuffer);
		return true;
	}

	public BoingReactorField()
	{
		Params.Init();
		m_bounds = Aabb.Empty;
		m_init = false;
	}

	public void Reboot()
	{
		m_gridCenter = base.transform.position;
		Vector3 vector = (m_qPrevGridCenterNorm = QuantizeNorm(m_gridCenter));
		switch (CellMoveMode)
		{
		case CellMoveModeEnum.Follow:
			m_gridCenter = base.transform.position;
			m_iCellBaseX = 0;
			m_iCellBaseY = 0;
			m_iCellBaseZ = 0;
			m_iCellBaseZ = 0;
			m_iCellBaseZ = 0;
			break;
		case CellMoveModeEnum.WrapAround:
			m_gridCenter = vector * CellSize;
			m_iCellBaseX = MathUtil.Modulo((int)m_qPrevGridCenterNorm.x, CellsX);
			m_iCellBaseY = MathUtil.Modulo((int)m_qPrevGridCenterNorm.y, CellsY);
			m_iCellBaseZ = MathUtil.Modulo((int)m_qPrevGridCenterNorm.z, CellsZ);
			break;
		}
	}

	public void OnEnable()
	{
		Reboot();
		BoingManager.Register(this);
	}

	public void Start()
	{
		Reboot();
		m_cellMoveMode = CellMoveMode;
	}

	public void OnDisable()
	{
		BoingManager.Unregister(this);
		DisposeCpuResources();
		DisposeGpuResources();
	}

	public void DisposeCpuResources()
	{
		m_aCpuCell = null;
	}

	public void DisposeGpuResources()
	{
		if (m_effectorIndexBuffer != null)
		{
			m_effectorIndexBuffer.Dispose();
			m_effectorIndexBuffer = null;
		}
		if (m_reactorParamsBuffer != null)
		{
			m_reactorParamsBuffer.Dispose();
			m_reactorParamsBuffer = null;
		}
		if (m_fieldParamsBuffer != null)
		{
			m_fieldParamsBuffer.Dispose();
			m_fieldParamsBuffer = null;
		}
		if (m_cellsBuffer != null)
		{
			m_cellsBuffer.Dispose();
			m_cellsBuffer = null;
		}
		if (m_cellsBuffer != null)
		{
			m_cellsBuffer.Dispose();
			m_cellsBuffer = null;
		}
	}

	public bool SampleCpuGrid(Vector3 p, out Vector3 positionOffset, out Vector4 rotationOffset)
	{
		bool flag = false;
		switch (FalloffDimensions)
		{
		case FalloffDimensionsEnum.XYZ:
			flag = m_bounds.Contains(p);
			break;
		case FalloffDimensionsEnum.XY:
			flag = m_bounds.ContainsX(p) && m_bounds.ContainsY(p);
			break;
		case FalloffDimensionsEnum.XZ:
			flag = m_bounds.ContainsX(p) && m_bounds.ContainsZ(p);
			break;
		case FalloffDimensionsEnum.YZ:
			flag = m_bounds.ContainsY(p) && m_bounds.ContainsZ(p);
			break;
		}
		if (!flag)
		{
			positionOffset = Vector3.zero;
			rotationOffset = QuaternionUtil.ToVector4(Quaternion.identity);
			return false;
		}
		float num = 0.5f * CellSize;
		Vector3 vector = p - (m_gridCenter + GetCellCenterOffset(0, 0, 0));
		Vector3 vector2 = QuantizeNorm(vector + new Vector3(0f - num, 0f - num, 0f - num));
		Vector3 vector3 = vector2 * CellSize;
		int num2 = Mathf.Clamp((int)vector2.x, 0, CellsX - 1);
		int num3 = Mathf.Clamp((int)vector2.y, 0, CellsY - 1);
		int num4 = Mathf.Clamp((int)vector2.z, 0, CellsZ - 1);
		int x = Mathf.Min(num2 + 1, CellsX - 1);
		int y = Mathf.Min(num3 + 1, CellsY - 1);
		int z = Mathf.Min(num4 + 1, CellsZ - 1);
		ResolveCellIndex(num2, num3, num4, 1, out var resX, out var resY, out var resZ);
		ResolveCellIndex(x, y, z, 1, out var resX2, out var resY2, out var resZ2);
		bool lerpX = resX != resX2;
		bool lerpY = resY != resY2;
		bool lerpZ = resZ != resZ2;
		Vector3 vector4 = (vector - vector3) / CellSize;
		Vector3 vector5 = p - base.transform.position;
		switch (FalloffDimensions)
		{
		case FalloffDimensionsEnum.XY:
			vector5.z = 0f;
			break;
		case FalloffDimensionsEnum.XZ:
			vector5.y = 0f;
			break;
		case FalloffDimensionsEnum.YZ:
			vector5.x = 0f;
			break;
		}
		int num5 = Mathf.Max(CellsX, Mathf.Max(CellsY, CellsZ));
		float num6 = 1f;
		switch (FalloffMode)
		{
		case FalloffModeEnum.Circle:
		{
			float num7 = num * (float)num5;
			Vector3 vector10 = new Vector3((float)num5 / (float)CellsX, (float)num5 / (float)CellsY, (float)num5 / (float)CellsZ);
			vector5.x *= vector10.x;
			vector5.y *= vector10.y;
			vector5.z *= vector10.z;
			float magnitude = vector5.magnitude;
			float num8 = Mathf.Max(0f, FalloffRatio * num7 - num);
			float num9 = Mathf.Max(MathUtil.Epsilon, (1f - FalloffRatio) * num7 - num);
			num6 = 1f - Mathf.Clamp01((magnitude - num8) / num9);
			break;
		}
		case FalloffModeEnum.Square:
		{
			Vector3 vector6 = num * new Vector3(CellsX, CellsY, CellsZ);
			Vector3 vector7 = FalloffRatio * vector6 - num * Vector3.one;
			vector7.x = Mathf.Max(0f, vector7.x);
			vector7.y = Mathf.Max(0f, vector7.y);
			vector7.z = Mathf.Max(0f, vector7.z);
			Vector3 vector8 = (1f - FalloffRatio) * vector6 - num * Vector3.one;
			vector8.x = Mathf.Max(MathUtil.Epsilon, vector8.x);
			vector8.y = Mathf.Max(MathUtil.Epsilon, vector8.y);
			vector8.z = Mathf.Max(MathUtil.Epsilon, vector8.z);
			Vector3 vector9 = new Vector3(1f - Mathf.Clamp01((Mathf.Abs(vector5.x) - vector7.x) / vector8.x), 1f - Mathf.Clamp01((Mathf.Abs(vector5.y) - vector7.y) / vector8.y), 1f - Mathf.Clamp01((Mathf.Abs(vector5.z) - vector7.z) / vector8.z));
			switch (FalloffDimensions)
			{
			case FalloffDimensionsEnum.XY:
				vector9.x = 1f;
				break;
			case FalloffDimensionsEnum.XZ:
				vector9.y = 1f;
				break;
			case FalloffDimensionsEnum.YZ:
				vector9.z = 1f;
				break;
			}
			num6 = Mathf.Min(vector9.x, Mathf.Min(vector9.y, vector9.z));
			break;
		}
		}
		s_aCellOffset[0] = m_aCpuCell[resZ, resY, resX].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(num2, num3, num4);
		s_aCellOffset[1] = m_aCpuCell[resZ, resY, resX2].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(x, num3, num4);
		s_aCellOffset[2] = m_aCpuCell[resZ, resY2, resX].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(num2, y, num4);
		s_aCellOffset[3] = m_aCpuCell[resZ, resY2, resX2].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(x, y, num4);
		s_aCellOffset[4] = m_aCpuCell[resZ2, resY, resX].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(num2, num3, z);
		s_aCellOffset[5] = m_aCpuCell[resZ2, resY, resX2].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(x, num3, z);
		s_aCellOffset[6] = m_aCpuCell[resZ2, resY2, resX].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(num2, y, z);
		s_aCellOffset[7] = m_aCpuCell[resZ2, resY2, resX2].PositionSpring.Value - m_gridCenter - GetCellCenterOffset(x, y, z);
		positionOffset = VectorUtil.TriLerp(ref s_aCellOffset[0], ref s_aCellOffset[1], ref s_aCellOffset[2], ref s_aCellOffset[3], ref s_aCellOffset[4], ref s_aCellOffset[5], ref s_aCellOffset[6], ref s_aCellOffset[7], lerpX, lerpY, lerpZ, vector4.x, vector4.y, vector4.z);
		rotationOffset = VectorUtil.TriLerp(ref m_aCpuCell[resZ, resY, resX].RotationSpring.ValueVec, ref m_aCpuCell[resZ, resY, resX2].RotationSpring.ValueVec, ref m_aCpuCell[resZ, resY2, resX].RotationSpring.ValueVec, ref m_aCpuCell[resZ, resY2, resX2].RotationSpring.ValueVec, ref m_aCpuCell[resZ2, resY, resX].RotationSpring.ValueVec, ref m_aCpuCell[resZ2, resY, resX2].RotationSpring.ValueVec, ref m_aCpuCell[resZ2, resY2, resX].RotationSpring.ValueVec, ref m_aCpuCell[resZ2, resY2, resX2].RotationSpring.ValueVec, lerpX, lerpY, lerpZ, vector4.x, vector4.y, vector4.z);
		positionOffset *= num6;
		rotationOffset = QuaternionUtil.ToVector4(QuaternionUtil.Pow(QuaternionUtil.FromVector4(rotationOffset), num6));
		return true;
	}

	private void UpdateFieldParamsGpu()
	{
		m_fieldParams.CellsX = CellsX;
		m_fieldParams.CellsY = CellsY;
		m_fieldParams.CellsZ = CellsZ;
		m_fieldParams.NumEffectors = 0;
		if (Effectors != null)
		{
			BoingEffector[] effectors = Effectors;
			foreach (BoingEffector boingEffector in effectors)
			{
				if (!(boingEffector == null))
				{
					BoingEffector component = boingEffector.GetComponent<BoingEffector>();
					if (!(component == null) && component.isActiveAndEnabled)
					{
						m_fieldParams.NumEffectors++;
					}
				}
			}
		}
		m_fieldParams.iCellBaseX = m_iCellBaseX;
		m_fieldParams.iCellBaseY = m_iCellBaseY;
		m_fieldParams.iCellBaseZ = m_iCellBaseZ;
		m_fieldParams.FalloffMode = (int)FalloffMode;
		m_fieldParams.FalloffDimensions = (int)FalloffDimensions;
		m_fieldParams.PropagationDepth = PropagationDepth;
		m_fieldParams.GridCenter = m_gridCenter;
		m_fieldParams.UpWs = (Params.Bits.IsBitSet(6) ? Params.RotationReactionUp : (base.transform.rotation * VectorUtil.NormalizeSafe(Params.RotationReactionUp, Vector3.up)));
		m_fieldParams.FieldPosition = base.transform.position;
		m_fieldParams.FalloffRatio = FalloffRatio;
		m_fieldParams.CellSize = CellSize;
		m_fieldParams.DeltaTime = Time.deltaTime;
		if (m_fieldParamsBuffer != null)
		{
			m_fieldParamsBuffer.SetData(new FieldParams[1] { m_fieldParams });
		}
	}

	private void UpdateFlags()
	{
		Params.Bits.SetBit(0, TwoDDistanceCheck);
		Params.Bits.SetBit(1, TwoDPositionInfluence);
		Params.Bits.SetBit(2, TwoDRotationInfluence);
		Params.Bits.SetBit(3, EnablePositionEffect);
		Params.Bits.SetBit(4, EnableRotationEffect);
		Params.Bits.SetBit(6, GlobalReactionUpVector);
		Params.Bits.SetBit(7, EnablePropagation);
		Params.Bits.SetBit(8, AnchorPropagationAtBorder);
	}

	public void UpdateBounds()
	{
		m_bounds = new Aabb(m_gridCenter + GetCellCenterOffset(0, 0, 0), m_gridCenter + GetCellCenterOffset(CellsX - 1, CellsY - 1, CellsZ - 1));
		m_bounds.Expand(CellSize);
	}

	public void PrepareExecute()
	{
		Init();
		if (SharedParams != null)
		{
			BoingWork.Params.Copy(ref SharedParams.Params, ref Params);
		}
		UpdateFlags();
		UpdateBounds();
		if (m_hardwareMode != HardwareMode)
		{
			switch (m_hardwareMode)
			{
			case HardwareModeEnum.CPU:
				DisposeCpuResources();
				break;
			case HardwareModeEnum.GPU:
				DisposeGpuResources();
				break;
			}
			m_hardwareMode = HardwareMode;
		}
		switch (m_hardwareMode)
		{
		case HardwareModeEnum.CPU:
			ValidateCpuResources();
			break;
		case HardwareModeEnum.GPU:
			ValidateGpuResources();
			break;
		}
		HandleCellMove();
		switch (m_hardwareMode)
		{
		case HardwareModeEnum.CPU:
			FinishPrepareExecuteCpu();
			break;
		case HardwareModeEnum.GPU:
			FinishPrepareExecuteGpu();
			break;
		}
	}

	private void ValidateCpuResources()
	{
		CellsX = Mathf.Max(1, CellsX);
		CellsY = Mathf.Max(1, CellsY);
		CellsZ = Mathf.Max(1, CellsZ);
		if (m_aCpuCell != null && m_cellsX == CellsX && m_cellsY == CellsY && m_cellsZ == CellsZ)
		{
			return;
		}
		m_aCpuCell = new BoingWork.Params.InstanceData[CellsZ, CellsY, CellsX];
		for (int i = 0; i < CellsZ; i++)
		{
			for (int j = 0; j < CellsY; j++)
			{
				for (int k = 0; k < CellsX; k++)
				{
					ResolveCellIndex(k, j, i, -1, out var resX, out var resY, out var resZ);
					m_aCpuCell[i, j, k].Reset(m_gridCenter + GetCellCenterOffset(resX, resY, resZ), instantAccumulation: false);
				}
			}
		}
		m_cellsX = CellsX;
		m_cellsY = CellsY;
		m_cellsZ = CellsZ;
	}

	private void ValidateGpuResources()
	{
		bool flag = false;
		bool flag2 = m_shader == null || s_computeKernelId == null;
		if (flag2)
		{
			m_shader = Resources.Load<ComputeShader>("Boing Kit/BoingReactorFieldCompute");
			flag = true;
			if (s_computeKernelId == null)
			{
				s_computeKernelId = new ComputeKernelId();
				s_computeKernelId.InitKernel = m_shader.FindKernel("Init");
				s_computeKernelId.MoveKernel = m_shader.FindKernel("Move");
				s_computeKernelId.WrapXKernel = m_shader.FindKernel("WrapX");
				s_computeKernelId.WrapYKernel = m_shader.FindKernel("WrapY");
				s_computeKernelId.WrapZKernel = m_shader.FindKernel("WrapZ");
				s_computeKernelId.ExecuteKernel = m_shader.FindKernel("Execute");
			}
		}
		bool flag3 = m_effectorIndexBuffer == null || (Effectors != null && m_numEffectors != Effectors.Length);
		if (flag3 && Effectors != null)
		{
			if (m_effectorIndexBuffer != null)
			{
				m_effectorIndexBuffer.Dispose();
			}
			m_effectorIndexBuffer = new ComputeBuffer(Effectors.Length, 4);
			flag = true;
			m_numEffectors = Effectors.Length;
		}
		if (flag2 || flag3)
		{
			m_shader.SetBuffer(s_computeKernelId.ExecuteKernel, ShaderPropertyId.EffectorIndices, m_effectorIndexBuffer);
		}
		bool flag4 = m_reactorParamsBuffer == null;
		if (flag4)
		{
			m_reactorParamsBuffer = new ComputeBuffer(1, BoingWork.Params.Stride);
			flag = true;
		}
		if (flag2 || flag4)
		{
			m_shader.SetBuffer(s_computeKernelId.ExecuteKernel, ShaderPropertyId.ReactorParams, m_reactorParamsBuffer);
		}
		bool flag5 = m_fieldParamsBuffer == null;
		if (flag5)
		{
			m_fieldParamsBuffer = new ComputeBuffer(1, FieldParams.Stride);
			flag = true;
		}
		if (flag2 || flag5)
		{
			m_shader.SetBuffer(s_computeKernelId.InitKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
			m_shader.SetBuffer(s_computeKernelId.MoveKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapXKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapYKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapZKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
			m_shader.SetBuffer(s_computeKernelId.ExecuteKernel, ShaderPropertyId.ComputeFieldParams, m_fieldParamsBuffer);
		}
		m_cellBufferNeedsReset = m_cellsBuffer == null || m_cellsX != CellsX || m_cellsY != CellsY || m_cellsZ != CellsZ;
		if (m_cellBufferNeedsReset)
		{
			if (m_cellsBuffer != null)
			{
				m_cellsBuffer.Dispose();
			}
			int num = CellsX * CellsY * CellsZ;
			m_cellsBuffer = new ComputeBuffer(num, BoingWork.Params.InstanceData.Stride);
			BoingWork.Params.InstanceData[] array = new BoingWork.Params.InstanceData[num];
			for (int i = 0; i < num; i++)
			{
				array[i].PositionSpring.Reset();
				array[i].RotationSpring.Reset();
			}
			m_cellsBuffer.SetData(array);
			flag = true;
			m_cellsX = CellsX;
			m_cellsY = CellsY;
			m_cellsZ = CellsZ;
		}
		if (flag2 || m_cellBufferNeedsReset)
		{
			m_shader.SetBuffer(s_computeKernelId.InitKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
			m_shader.SetBuffer(s_computeKernelId.MoveKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapXKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapYKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
			m_shader.SetBuffer(s_computeKernelId.WrapZKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
			m_shader.SetBuffer(s_computeKernelId.ExecuteKernel, ShaderPropertyId.ComputeCells, m_cellsBuffer);
		}
		if (flag)
		{
			m_gpuResourceSetId++;
			if (m_gpuResourceSetId < 0)
			{
				m_gpuResourceSetId = -1;
			}
		}
	}

	private void FinishPrepareExecuteCpu()
	{
		Quaternion rotation = base.transform.rotation;
		for (int i = 0; i < CellsZ; i++)
		{
			for (int j = 0; j < CellsY; j++)
			{
				for (int k = 0; k < CellsX; k++)
				{
					ResolveCellIndex(k, j, i, -1, out var resX, out var resY, out var resZ);
					m_aCpuCell[i, j, k].PrepareExecute(ref Params, m_gridCenter, rotation, GetCellCenterOffset(resX, resY, resZ));
				}
			}
		}
	}

	private void FinishPrepareExecuteGpu()
	{
		if (m_cellBufferNeedsReset)
		{
			UpdateFieldParamsGpu();
			m_shader.Dispatch(s_computeKernelId.InitKernel, CellsX, CellsY, CellsZ);
		}
	}

	public void Init()
	{
		if (!m_init)
		{
			m_hardwareMode = HardwareMode;
			m_init = true;
		}
	}

	public void Sanitize()
	{
		if (PropagationDepth < 0)
		{
			Debug.LogWarning("Propagation iterations must be a positive number.");
		}
		else if (PropagationDepth > 3)
		{
			Debug.LogWarning("For performance reasons, propagation is limited to 3 iterations.");
		}
		PropagationDepth = Mathf.Clamp(PropagationDepth, 1, 3);
	}

	public void HandleCellMove()
	{
		if (m_cellMoveMode != CellMoveMode)
		{
			Reboot();
			m_cellMoveMode = CellMoveMode;
		}
		switch (CellMoveMode)
		{
		case CellMoveModeEnum.Follow:
		{
			Vector3 vector2 = base.transform.position - m_gridCenter;
			switch (HardwareMode)
			{
			case HardwareModeEnum.CPU:
			{
				for (int i = 0; i < CellsZ; i++)
				{
					for (int j = 0; j < CellsY; j++)
					{
						for (int k = 0; k < CellsX; k++)
						{
							m_aCpuCell[i, j, k].PositionSpring.Value += vector2;
						}
					}
				}
				break;
			}
			case HardwareModeEnum.GPU:
				UpdateFieldParamsGpu();
				m_shader.SetVector(ShaderPropertyId.MoveParams, vector2);
				m_shader.Dispatch(s_computeKernelId.MoveKernel, CellsX, CellsY, CellsZ);
				break;
			}
			m_gridCenter = base.transform.position;
			m_qPrevGridCenterNorm = QuantizeNorm(m_gridCenter);
			break;
		}
		case CellMoveModeEnum.WrapAround:
		{
			m_gridCenter = base.transform.position;
			Vector3 vector = QuantizeNorm(m_gridCenter);
			m_gridCenter = vector * CellSize;
			int num = (int)(vector.x - m_qPrevGridCenterNorm.x);
			int num2 = (int)(vector.y - m_qPrevGridCenterNorm.y);
			int num3 = (int)(vector.z - m_qPrevGridCenterNorm.z);
			m_qPrevGridCenterNorm = vector;
			if (num != 0 || num2 != 0 || num3 != 0)
			{
				switch (m_hardwareMode)
				{
				case HardwareModeEnum.CPU:
					WrapCpu(num, num2, num3);
					break;
				case HardwareModeEnum.GPU:
					WrapGpu(num, num2, num3);
					break;
				}
				m_iCellBaseX = MathUtil.Modulo(m_iCellBaseX + num, CellsX);
				m_iCellBaseY = MathUtil.Modulo(m_iCellBaseY + num2, CellsY);
				m_iCellBaseZ = MathUtil.Modulo(m_iCellBaseZ + num3, CellsZ);
			}
			break;
		}
		}
	}

	private void InitPropagationCpu(ref BoingWork.Params.InstanceData data)
	{
		data.PositionPropagationWorkData = Vector3.zero;
		data.RotationPropagationWorkData = Vector3.zero;
	}

	private void PropagateSpringCpu(ref BoingWork.Params.InstanceData data, float dt)
	{
		data.PositionSpring.Velocity += kPropagationFactor * PositionPropagation * data.PositionPropagationWorkData * dt;
		data.RotationSpring.VelocityVec += kPropagationFactor * RotationPropagation * data.RotationPropagationWorkData * dt;
	}

	private void ExtendPropagationBorder(ref BoingWork.Params.InstanceData data, float weight, int adjDeltaX, int adjDeltaY, int adjDeltaZ)
	{
		data.PositionPropagationWorkData += weight * (data.PositionOrigin + new Vector3(adjDeltaX, adjDeltaY, adjDeltaZ) * CellSize);
		data.RotationPropagationWorkData += weight * data.RotationOrigin;
	}

	private void AccumulatePropagationWeightedNeighbor(ref BoingWork.Params.InstanceData data, ref BoingWork.Params.InstanceData neighbor, float weight)
	{
		data.PositionPropagationWorkData += weight * (neighbor.PositionSpring.Value - neighbor.PositionOrigin);
		data.RotationPropagationWorkData += weight * (neighbor.RotationSpring.ValueVec - neighbor.RotationOrigin);
	}

	private void GatherPropagation(ref BoingWork.Params.InstanceData data, float weightSum)
	{
		data.PositionPropagationWorkData = data.PositionPropagationWorkData / weightSum - (data.PositionSpring.Value - data.PositionOrigin);
		data.RotationPropagationWorkData = data.RotationPropagationWorkData / weightSum - (data.RotationSpring.ValueVec - data.RotationOrigin);
	}

	private void AnchorPropagationBorder(ref BoingWork.Params.InstanceData data)
	{
		data.PositionPropagationWorkData = Vector3.zero;
		data.RotationPropagationWorkData = Vector3.zero;
	}

	private void PropagateCpu(float dt)
	{
		int[] array = new int[PropagationDepth * 2 + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i - PropagationDepth;
		}
		for (int j = 0; j < CellsZ; j++)
		{
			for (int k = 0; k < CellsY; k++)
			{
				for (int l = 0; l < CellsX; l++)
				{
					InitPropagationCpu(ref m_aCpuCell[j, k, l]);
				}
			}
		}
		for (int m = 0; m < CellsZ; m++)
		{
			for (int n = 0; n < CellsY; n++)
			{
				for (int num = 0; num < CellsX; num++)
				{
					ResolveCellIndex(num, n, m, -1, out var resX, out var resY, out var resZ);
					float num2 = 0f;
					int[] array2 = array;
					foreach (int num4 in array2)
					{
						int[] array3 = array;
						foreach (int num6 in array3)
						{
							int[] array4 = array;
							foreach (int num8 in array4)
							{
								if (num8 != 0 || num6 != 0 || num4 != 0)
								{
									int num9 = num8 * num8 + num6 * num6 + num4 * num4;
									float num10 = s_aSqrtInv[num9];
									num2 += num10;
									if ((CellsX <= 2 || ((resX != 0 || num8 >= 0) && (resX != CellsX - 1 || num8 <= 0))) && (CellsY <= 2 || ((resY != 0 || num6 >= 0) && (resY != CellsY - 1 || num6 <= 0))) && (CellsZ <= 2 || ((resZ != 0 || num4 >= 0) && (resZ != CellsZ - 1 || num4 <= 0))))
									{
										int num11 = MathUtil.Modulo(num + num8, CellsX);
										int num12 = MathUtil.Modulo(n + num6, CellsY);
										int num13 = MathUtil.Modulo(m + num4, CellsZ);
										AccumulatePropagationWeightedNeighbor(ref m_aCpuCell[m, n, num], ref m_aCpuCell[num13, num12, num11], num10);
									}
								}
							}
						}
					}
					if (!(num2 <= 0f))
					{
						GatherPropagation(ref m_aCpuCell[m, n, num], num2);
					}
				}
			}
		}
		if (AnchorPropagationAtBorder)
		{
			for (int num14 = 0; num14 < CellsZ; num14++)
			{
				for (int num15 = 0; num15 < CellsY; num15++)
				{
					for (int num16 = 0; num16 < CellsX; num16++)
					{
						ResolveCellIndex(num16, num15, num14, -1, out var resX2, out var resY2, out var resZ2);
						if (((resX2 == 0 || resX2 == CellsX - 1) && CellsX > 2) || ((resY2 == 0 || resY2 == CellsY - 1) && CellsY > 2) || ((resZ2 == 0 || resZ2 == CellsZ - 1) && CellsZ > 2))
						{
							AnchorPropagationBorder(ref m_aCpuCell[num14, num15, num16]);
						}
					}
				}
			}
		}
		for (int num17 = 0; num17 < CellsZ; num17++)
		{
			for (int num18 = 0; num18 < CellsY; num18++)
			{
				for (int num19 = 0; num19 < CellsX; num19++)
				{
					PropagateSpringCpu(ref m_aCpuCell[num17, num18, num19], dt);
				}
			}
		}
	}

	private void WrapCpu(int deltaX, int deltaY, int deltaZ)
	{
		if (deltaX != 0)
		{
			int num = ((deltaX <= 0) ? 1 : (-1));
			for (int i = 0; i < CellsZ; i++)
			{
				for (int j = 0; j < CellsY; j++)
				{
					for (int k = ((deltaX > 0) ? (deltaX - 1) : (CellsX + deltaX)); k >= 0 && k < CellsX; k += num)
					{
						ResolveCellIndex(k, j, i, 1, out var resX, out var resY, out var resZ);
						ResolveCellIndex(resX - deltaX, resY - deltaY, resZ - deltaZ, -1, out var resX2, out var resY2, out var resZ2);
						m_aCpuCell[resZ, resY, resX].Reset(m_gridCenter + GetCellCenterOffset(resX2, resY2, resZ2), instantAccumulation: true);
					}
				}
			}
		}
		if (deltaY != 0)
		{
			int num2 = ((deltaY <= 0) ? 1 : (-1));
			for (int l = 0; l < CellsZ; l++)
			{
				for (int m = ((deltaY > 0) ? (deltaY - 1) : (CellsY + deltaY)); m >= 0 && m < CellsY; m += num2)
				{
					for (int n = 0; n < CellsX; n++)
					{
						ResolveCellIndex(n, m, l, 1, out var resX3, out var resY3, out var resZ3);
						ResolveCellIndex(resX3 - deltaX, resY3 - deltaY, resZ3 - deltaZ, -1, out var resX4, out var resY4, out var resZ4);
						m_aCpuCell[resZ3, resY3, resX3].Reset(m_gridCenter + GetCellCenterOffset(resX4, resY4, resZ4), instantAccumulation: true);
					}
				}
			}
		}
		if (deltaZ == 0)
		{
			return;
		}
		int num3 = ((deltaZ <= 0) ? 1 : (-1));
		for (int num4 = ((deltaZ > 0) ? (deltaZ - 1) : (CellsZ + deltaZ)); num4 >= 0 && num4 < CellsZ; num4 += num3)
		{
			for (int num5 = 0; num5 < CellsY; num5++)
			{
				for (int num6 = 0; num6 < CellsX; num6++)
				{
					ResolveCellIndex(num6, num5, num4, 1, out var resX5, out var resY5, out var resZ5);
					ResolveCellIndex(resX5 - deltaX, resY5 - deltaY, resZ5 - deltaZ, -1, out var resX6, out var resY6, out var resZ6);
					m_aCpuCell[resZ5, resY5, resX5].Reset(m_gridCenter + GetCellCenterOffset(resX6, resY6, resZ6), instantAccumulation: true);
				}
			}
		}
	}

	private void WrapGpu(int deltaX, int deltaY, int deltaZ)
	{
		UpdateFieldParamsGpu();
		m_shader.SetInts(ShaderPropertyId.WrapParams, deltaX, deltaY, deltaZ);
		if (deltaX != 0)
		{
			m_shader.Dispatch(s_computeKernelId.WrapXKernel, 1, CellsY, CellsZ);
		}
		if (deltaY != 0)
		{
			m_shader.Dispatch(s_computeKernelId.WrapYKernel, CellsX, 1, CellsZ);
		}
		if (deltaZ != 0)
		{
			m_shader.Dispatch(s_computeKernelId.WrapZKernel, CellsX, CellsY, 1);
		}
	}

	public void ExecuteCpu(float dt)
	{
		PrepareExecute();
		if (Effectors == null || Effectors.Length == 0)
		{
			return;
		}
		if (EnablePropagation)
		{
			PropagateCpu(dt);
		}
		BoingEffector[] effectors = Effectors;
		foreach (BoingEffector boingEffector in effectors)
		{
			if (boingEffector == null)
			{
				continue;
			}
			BoingEffector.Params effector = default(BoingEffector.Params);
			effector.Fill(boingEffector);
			if (!m_bounds.Intersects(ref effector))
			{
				continue;
			}
			for (int j = 0; j < CellsZ; j++)
			{
				for (int k = 0; k < CellsY; k++)
				{
					for (int l = 0; l < CellsX; l++)
					{
						m_aCpuCell[j, k, l].AccumulateTarget(ref Params, ref effector, dt);
					}
				}
			}
		}
		for (int m = 0; m < CellsZ; m++)
		{
			for (int n = 0; n < CellsY; n++)
			{
				for (int num = 0; num < CellsX; num++)
				{
					m_aCpuCell[m, n, num].EndAccumulateTargets(ref Params);
					m_aCpuCell[m, n, num].Execute(ref Params, dt);
				}
			}
		}
	}

	public void ExecuteGpu(float dt, ComputeBuffer effectorParamsBuffer, Dictionary<int, int> effectorParamsIndexMap)
	{
		PrepareExecute();
		UpdateFieldParamsGpu();
		m_shader.SetBuffer(s_computeKernelId.ExecuteKernel, ShaderPropertyId.Effectors, effectorParamsBuffer);
		if (m_fieldParams.NumEffectors > 0)
		{
			int[] array = new int[m_fieldParams.NumEffectors];
			int num = 0;
			BoingEffector[] effectors = Effectors;
			foreach (BoingEffector boingEffector in effectors)
			{
				if (!(boingEffector == null))
				{
					BoingEffector component = boingEffector.GetComponent<BoingEffector>();
					if (!(component == null) && component.isActiveAndEnabled && effectorParamsIndexMap.TryGetValue(component.GetInstanceID(), out var value))
					{
						array[num++] = value;
					}
				}
			}
			m_effectorIndexBuffer.SetData(array);
		}
		s_aReactorParams[0] = Params;
		m_reactorParamsBuffer.SetData(s_aReactorParams);
		m_shader.SetVector(ShaderPropertyId.PropagationParams, new Vector4(PositionPropagation, RotationPropagation, kPropagationFactor, 0f));
		m_shader.Dispatch(s_computeKernelId.ExecuteKernel, CellsX, CellsY, CellsZ);
	}

	public void OnDrawGizmosSelected()
	{
		if (base.isActiveAndEnabled)
		{
			DrawGizmos(drawEffectors: true);
		}
	}

	private void DrawGizmos(bool drawEffectors)
	{
		Vector3 vector = GetGridCenter();
		switch (CellMoveMode)
		{
		case CellMoveModeEnum.Follow:
			vector = base.transform.position;
			break;
		case CellMoveModeEnum.WrapAround:
			vector = new Vector3(Mathf.Round(base.transform.position.x / CellSize), Mathf.Round(base.transform.position.y / CellSize), Mathf.Round(base.transform.position.z / CellSize)) * CellSize;
			break;
		}
		BoingWork.Params.InstanceData[,,] array = null;
		switch (HardwareMode)
		{
		case HardwareModeEnum.CPU:
			array = m_aCpuCell;
			break;
		case HardwareModeEnum.GPU:
			if (m_cellsBuffer != null)
			{
				array = new BoingWork.Params.InstanceData[CellsZ, CellsY, CellsX];
				m_cellsBuffer.GetData(array);
			}
			break;
		}
		int num = 1;
		if (CellsX * CellsY * CellsZ > 1024)
		{
			num = 2;
		}
		if (CellsX * CellsY * CellsZ > 4096)
		{
			num = 3;
		}
		if (CellsX * CellsY * CellsZ > 8192)
		{
			num = 4;
		}
		for (int i = 0; i < CellsZ; i++)
		{
			for (int j = 0; j < CellsY; j++)
			{
				for (int k = 0; k < CellsX; k++)
				{
					ResolveCellIndex(k, j, i, -1, out var resX, out var resY, out var resZ);
					Vector3 center = vector + GetCellCenterOffset(resX, resY, resZ);
					if (array != null && k % num == 0 && j % num == 0 && i % num == 0)
					{
						BoingWork.Params.InstanceData instanceData = array[i, j, k];
						Gizmos.color = new Color(1f, 1f, 1f, 1f);
						Gizmos.matrix = Matrix4x4.TRS(instanceData.PositionSpring.Value, instanceData.RotationSpring.ValueQuat, Vector3.one);
						Gizmos.DrawCube(Vector3.zero, Mathf.Min(0.1f, 0.5f * CellSize) * Vector3.one);
						Gizmos.matrix = Matrix4x4.identity;
					}
					Gizmos.color = new Color(1f, 0.5f, 0.2f, 1f);
					Gizmos.DrawWireCube(center, CellSize * Vector3.one);
				}
			}
		}
		switch (FalloffMode)
		{
		case FalloffModeEnum.Circle:
		{
			float num2 = Mathf.Max(CellsX, Mathf.Max(CellsY, CellsZ));
			Gizmos.color = new Color(1f, 1f, 0.2f, 0.5f);
			Gizmos.matrix = Matrix4x4.Translate(vector) * Matrix4x4.Scale(new Vector3(CellsX, CellsY, CellsZ) / num2);
			Gizmos.DrawWireSphere(Vector3.zero, 0.5f * CellSize * num2 * FalloffRatio);
			Gizmos.matrix = Matrix4x4.identity;
			break;
		}
		case FalloffModeEnum.Square:
		{
			Vector3 size = CellSize * FalloffRatio * new Vector3(CellsX, CellsY, CellsZ);
			Gizmos.color = new Color(1f, 1f, 0.2f, 0.5f);
			Gizmos.DrawWireCube(vector, size);
			break;
		}
		}
		if (!drawEffectors || Effectors == null)
		{
			return;
		}
		BoingEffector[] effectors = Effectors;
		foreach (BoingEffector boingEffector in effectors)
		{
			if (!(boingEffector == null))
			{
				boingEffector.OnDrawGizmosSelected();
			}
		}
	}

	private Vector3 GetGridCenter()
	{
		return CellMoveMode switch
		{
			CellMoveModeEnum.Follow => base.transform.position, 
			CellMoveModeEnum.WrapAround => QuantizeNorm(base.transform.position) * CellSize, 
			_ => base.transform.position, 
		};
	}

	private Vector3 QuantizeNorm(Vector3 p)
	{
		return new Vector3(Mathf.Round(p.x / CellSize), Mathf.Round(p.y / CellSize), Mathf.Round(p.z / CellSize));
	}

	private Vector3 GetCellCenterOffset(int x, int y, int z)
	{
		return CellSize * (-0.5f * (new Vector3(CellsX, CellsY, CellsZ) - Vector3.one) + new Vector3(x, y, z));
	}

	private void ResolveCellIndex(int x, int y, int z, int baseMult, out int resX, out int resY, out int resZ)
	{
		resX = MathUtil.Modulo(x + baseMult * m_iCellBaseX, CellsX);
		resY = MathUtil.Modulo(y + baseMult * m_iCellBaseY, CellsY);
		resZ = MathUtil.Modulo(z + baseMult * m_iCellBaseZ, CellsZ);
	}
}
