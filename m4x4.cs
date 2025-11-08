using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct m4x4
{
	[FieldOffset(0)]
	[NonSerialized]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
	public unsafe fixed float data_f[16];

	[FieldOffset(0)]
	[NonSerialized]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
	public unsafe fixed int data_i[16];

	[FieldOffset(0)]
	[NonSerialized]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
	public unsafe fixed ushort data_h[32];

	[FieldOffset(0)]
	[NonSerialized]
	public Vector4 r0;

	[FieldOffset(16)]
	[NonSerialized]
	public Vector4 r1;

	[FieldOffset(32)]
	[NonSerialized]
	public Vector4 r2;

	[FieldOffset(48)]
	[NonSerialized]
	public Vector4 r3;

	[FieldOffset(0)]
	[NonSerialized]
	public float m00;

	[FieldOffset(4)]
	[NonSerialized]
	public float m01;

	[FieldOffset(8)]
	[NonSerialized]
	public float m02;

	[FieldOffset(12)]
	[NonSerialized]
	public float m03;

	[FieldOffset(16)]
	[NonSerialized]
	public float m10;

	[FieldOffset(20)]
	[NonSerialized]
	public float m11;

	[FieldOffset(24)]
	[NonSerialized]
	public float m12;

	[FieldOffset(28)]
	[NonSerialized]
	public float m13;

	[FieldOffset(32)]
	[NonSerialized]
	public float m20;

	[FieldOffset(36)]
	[NonSerialized]
	public float m21;

	[FieldOffset(40)]
	[NonSerialized]
	public float m22;

	[FieldOffset(44)]
	[NonSerialized]
	public float m23;

	[FieldOffset(48)]
	[NonSerialized]
	public float m30;

	[FieldOffset(52)]
	[NonSerialized]
	public float m31;

	[FieldOffset(56)]
	[NonSerialized]
	public float m32;

	[FieldOffset(60)]
	[NonSerialized]
	public float m33;

	[FieldOffset(0)]
	[HideInInspector]
	public int i00;

	[FieldOffset(4)]
	[HideInInspector]
	public int i01;

	[FieldOffset(8)]
	[HideInInspector]
	public int i02;

	[FieldOffset(12)]
	[HideInInspector]
	public int i03;

	[FieldOffset(16)]
	[HideInInspector]
	public int i10;

	[FieldOffset(20)]
	[HideInInspector]
	public int i11;

	[FieldOffset(24)]
	[HideInInspector]
	public int i12;

	[FieldOffset(28)]
	[HideInInspector]
	public int i13;

	[FieldOffset(32)]
	[HideInInspector]
	public int i20;

	[FieldOffset(36)]
	[HideInInspector]
	public int i21;

	[FieldOffset(40)]
	[HideInInspector]
	public int i22;

	[FieldOffset(44)]
	[HideInInspector]
	public int i23;

	[FieldOffset(48)]
	[HideInInspector]
	public int i30;

	[FieldOffset(52)]
	[HideInInspector]
	public int i31;

	[FieldOffset(56)]
	[HideInInspector]
	public int i32;

	[FieldOffset(60)]
	[HideInInspector]
	public int i33;

	[FieldOffset(0)]
	[NonSerialized]
	public ushort h00_a;

	[FieldOffset(2)]
	[NonSerialized]
	public ushort h00_b;

	[FieldOffset(4)]
	[NonSerialized]
	public ushort h01_a;

	[FieldOffset(6)]
	[NonSerialized]
	public ushort h01_b;

	[FieldOffset(8)]
	[NonSerialized]
	public ushort h02_a;

	[FieldOffset(10)]
	[NonSerialized]
	public ushort h02_b;

	[FieldOffset(12)]
	[NonSerialized]
	public ushort h03_a;

	[FieldOffset(14)]
	[NonSerialized]
	public ushort h03_b;

	[FieldOffset(16)]
	[NonSerialized]
	public ushort h10_a;

	[FieldOffset(18)]
	[NonSerialized]
	public ushort h10_b;

	[FieldOffset(20)]
	[NonSerialized]
	public ushort h11_a;

	[FieldOffset(22)]
	[NonSerialized]
	public ushort h11_b;

	[FieldOffset(24)]
	[NonSerialized]
	public ushort h12_a;

	[FieldOffset(26)]
	[NonSerialized]
	public ushort h12_b;

	[FieldOffset(28)]
	[NonSerialized]
	public ushort h13_a;

	[FieldOffset(30)]
	[NonSerialized]
	public ushort h13_b;

	[FieldOffset(32)]
	[NonSerialized]
	public ushort h20_a;

	[FieldOffset(34)]
	[NonSerialized]
	public ushort h20_b;

	[FieldOffset(36)]
	[NonSerialized]
	public ushort h21_a;

	[FieldOffset(38)]
	[NonSerialized]
	public ushort h21_b;

	[FieldOffset(40)]
	[NonSerialized]
	public ushort h22_a;

	[FieldOffset(42)]
	[NonSerialized]
	public ushort h22_b;

	[FieldOffset(44)]
	[NonSerialized]
	public ushort h23_a;

	[FieldOffset(46)]
	[NonSerialized]
	public ushort h23_b;

	[FieldOffset(48)]
	[NonSerialized]
	public ushort h30_a;

	[FieldOffset(50)]
	[NonSerialized]
	public ushort h30_b;

	[FieldOffset(52)]
	[NonSerialized]
	public ushort h31_a;

	[FieldOffset(54)]
	[NonSerialized]
	public ushort h31_b;

	[FieldOffset(56)]
	[NonSerialized]
	public ushort h32_a;

	[FieldOffset(58)]
	[NonSerialized]
	public ushort h32_b;

	[FieldOffset(60)]
	[NonSerialized]
	public ushort h33_a;

	[FieldOffset(62)]
	[NonSerialized]
	public ushort h33_b;

	public m4x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
	{
		this = default(m4x4);
		this.m00 = m00;
		this.m01 = m01;
		this.m02 = m02;
		this.m03 = m03;
		this.m10 = m10;
		this.m11 = m11;
		this.m12 = m12;
		this.m13 = m13;
		this.m20 = m20;
		this.m21 = m21;
		this.m22 = m22;
		this.m23 = m23;
		this.m30 = m30;
		this.m31 = m31;
		this.m32 = m32;
		this.m33 = m33;
	}

	public m4x4(Vector4 row0, Vector4 row1, Vector4 row2, Vector4 row3)
	{
		this = default(m4x4);
		r0 = row0;
		r1 = row1;
		r2 = row2;
		r3 = row3;
	}

	public void Clear()
	{
		m00 = 0f;
		m01 = 0f;
		m02 = 0f;
		m03 = 0f;
		m10 = 0f;
		m11 = 0f;
		m12 = 0f;
		m13 = 0f;
		m20 = 0f;
		m21 = 0f;
		m22 = 0f;
		m23 = 0f;
		m30 = 0f;
		m31 = 0f;
		m32 = 0f;
		m33 = 0f;
	}

	public void SetRow0(ref Vector4 v)
	{
		m00 = v.x;
		m01 = v.y;
		m02 = v.z;
		m03 = v.w;
	}

	public void SetRow1(ref Vector4 v)
	{
		m10 = v.x;
		m11 = v.y;
		m12 = v.z;
		m13 = v.w;
	}

	public void SetRow2(ref Vector4 v)
	{
		m20 = v.x;
		m21 = v.y;
		m22 = v.z;
		m23 = v.w;
	}

	public void SetRow3(ref Vector4 v)
	{
		m30 = v.x;
		m31 = v.y;
		m32 = v.z;
		m33 = v.w;
	}

	public void Transpose()
	{
		float num = m01;
		float num2 = m02;
		float num3 = m03;
		float num4 = m10;
		float num5 = m12;
		float num6 = m13;
		float num7 = m20;
		float num8 = m21;
		float num9 = m23;
		float num10 = m30;
		float num11 = m31;
		float num12 = m32;
		m01 = num4;
		m02 = num7;
		m03 = num10;
		m10 = num;
		m12 = num8;
		m13 = num11;
		m20 = num2;
		m21 = num5;
		m23 = num12;
		m30 = num3;
		m31 = num6;
		m32 = num9;
	}

	public void Set(ref Vector4 row0, ref Vector4 row1, ref Vector4 row2, ref Vector4 row3)
	{
		r0 = row0;
		r1 = row1;
		r2 = row2;
		r3 = row3;
	}

	public void SetTransposed(ref Vector4 row0, ref Vector4 row1, ref Vector4 row2, ref Vector4 row3)
	{
		m00 = row0.x;
		m01 = row1.x;
		m02 = row2.x;
		m03 = row3.x;
		m10 = row0.y;
		m11 = row1.y;
		m12 = row2.y;
		m13 = row3.y;
		m20 = row0.z;
		m21 = row1.z;
		m22 = row2.z;
		m23 = row3.z;
		m30 = row0.w;
		m31 = row1.w;
		m32 = row2.w;
		m33 = row3.w;
	}

	public void Set(ref Matrix4x4 x)
	{
		m00 = x.m00;
		m01 = x.m01;
		m02 = x.m02;
		m03 = x.m03;
		m10 = x.m10;
		m11 = x.m11;
		m12 = x.m12;
		m13 = x.m13;
		m20 = x.m20;
		m21 = x.m21;
		m22 = x.m22;
		m23 = x.m23;
		m30 = x.m30;
		m31 = x.m31;
		m32 = x.m32;
		m33 = x.m33;
	}

	public void SetTransposed(ref Matrix4x4 x)
	{
		m00 = x.m00;
		m01 = x.m10;
		m02 = x.m20;
		m03 = x.m30;
		m10 = x.m01;
		m11 = x.m11;
		m12 = x.m21;
		m13 = x.m31;
		m20 = x.m02;
		m21 = x.m12;
		m22 = x.m22;
		m23 = x.m32;
		m30 = x.m03;
		m31 = x.m13;
		m32 = x.m23;
		m33 = x.m33;
	}

	public void Push(ref Matrix4x4 x)
	{
		x.m00 = m00;
		x.m01 = m01;
		x.m02 = m02;
		x.m03 = m03;
		x.m10 = m10;
		x.m11 = m11;
		x.m12 = m12;
		x.m13 = m13;
		x.m20 = m20;
		x.m21 = m21;
		x.m22 = m22;
		x.m23 = m23;
		x.m30 = m30;
		x.m31 = m31;
		x.m32 = m32;
		x.m33 = m33;
	}

	public void PushTransposed(ref Matrix4x4 x)
	{
		x.m00 = m00;
		x.m01 = m10;
		x.m02 = m20;
		x.m03 = m30;
		x.m10 = m01;
		x.m11 = m11;
		x.m12 = m21;
		x.m13 = m31;
		x.m20 = m02;
		x.m21 = m12;
		x.m22 = m22;
		x.m23 = m32;
		x.m30 = m03;
		x.m31 = m13;
		x.m32 = m23;
		x.m33 = m33;
	}

	public static ref m4x4 From(ref Matrix4x4 src)
	{
		return ref Unsafe.As<Matrix4x4, m4x4>(ref src);
	}
}
