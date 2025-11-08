using System;

[Flags]
public enum GestureHandState : uint
{
	None = 0u,
	IsLeft = 1u,
	IsRight = 2u,
	Open = 4u,
	Closed = 8u
}
