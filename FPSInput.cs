using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class FPSInput
{
	public static bool IsMouseCaptured => Cursor.lockState == CursorLockMode.Locked;

	public static bool LeftButton
	{
		get
		{
			if (Mouse.current != null)
			{
				return Mouse.current.leftButton.isPressed;
			}
			return false;
		}
	}

	public static bool LeftButtonDown
	{
		get
		{
			if (Mouse.current != null)
			{
				return Mouse.current.leftButton.wasPressedThisFrame;
			}
			return false;
		}
	}

	public static bool RightButton
	{
		get
		{
			if (Mouse.current != null)
			{
				return Mouse.current.rightButton.isPressed;
			}
			return false;
		}
	}

	public static bool RightButtonDown
	{
		get
		{
			if (Mouse.current != null)
			{
				return Mouse.current.rightButton.wasPressedThisFrame;
			}
			return false;
		}
	}

	public static Vector2 MouseDelta
	{
		get
		{
			if (Mouse.current == null)
			{
				return Vector2.zero;
			}
			return Mouse.current.delta.ReadValue();
		}
	}

	public static Vector2 MousePosition
	{
		get
		{
			if (Mouse.current == null)
			{
				return Vector2.zero;
			}
			return Mouse.current.position.ReadValue();
		}
	}

	public static void SetMouseCaptured(bool captured)
	{
		Cursor.lockState = (captured ? CursorLockMode.Locked : CursorLockMode.None);
	}

	public static bool IsPressed(FpsKey key)
	{
		return GetControl(key)?.isPressed ?? false;
	}

	public static bool WasPressedThisFrame(FpsKey key)
	{
		return GetControl(key)?.wasPressedThisFrame ?? false;
	}

	public static bool WasReleasedThisFrame(FpsKey key)
	{
		return GetControl(key)?.wasReleasedThisFrame ?? false;
	}

	public static float Value(FpsKey key)
	{
		if (!IsPressed(key))
		{
			return 0f;
		}
		return 1f;
	}

	private static ButtonControl GetControl(FpsKey key)
	{
		Mouse current = Mouse.current;
		switch (key)
		{
		case FpsKey.LMouse:
			return current?.leftButton;
		case FpsKey.RMouse:
			return current?.rightButton;
		case FpsKey.MMouse:
			return current?.middleButton;
		default:
		{
			Keyboard current2 = Keyboard.current;
			if (current2 == null)
			{
				return null;
			}
			switch (key)
			{
			case FpsKey.Shift:
				return current2.shiftKey;
			case FpsKey.Ctrl:
				return current2.ctrlKey;
			case FpsKey.Alt:
				return current2.altKey;
			default:
			{
				Key key2 = ToInputSystemKey(key);
				if (key2 != Key.None)
				{
					return current2[key2];
				}
				return null;
			}
			}
		}
		}
	}

	private static Key ToInputSystemKey(FpsKey key)
	{
		return key switch
		{
			FpsKey.A => Key.A, 
			FpsKey.B => Key.B, 
			FpsKey.C => Key.C, 
			FpsKey.D => Key.D, 
			FpsKey.E => Key.E, 
			FpsKey.F => Key.F, 
			FpsKey.G => Key.G, 
			FpsKey.H => Key.H, 
			FpsKey.I => Key.I, 
			FpsKey.J => Key.J, 
			FpsKey.K => Key.K, 
			FpsKey.L => Key.L, 
			FpsKey.M => Key.M, 
			FpsKey.N => Key.N, 
			FpsKey.O => Key.O, 
			FpsKey.P => Key.P, 
			FpsKey.Q => Key.Q, 
			FpsKey.R => Key.R, 
			FpsKey.S => Key.S, 
			FpsKey.T => Key.T, 
			FpsKey.U => Key.U, 
			FpsKey.V => Key.V, 
			FpsKey.W => Key.W, 
			FpsKey.X => Key.X, 
			FpsKey.Y => Key.Y, 
			FpsKey.Z => Key.Z, 
			FpsKey.Digit0 => Key.Digit0, 
			FpsKey.Digit1 => Key.Digit1, 
			FpsKey.Digit2 => Key.Digit2, 
			FpsKey.Digit3 => Key.Digit3, 
			FpsKey.Digit4 => Key.Digit4, 
			FpsKey.Digit5 => Key.Digit5, 
			FpsKey.Digit6 => Key.Digit6, 
			FpsKey.Digit7 => Key.Digit7, 
			FpsKey.Digit8 => Key.Digit8, 
			FpsKey.Digit9 => Key.Digit9, 
			FpsKey.F1 => Key.F1, 
			FpsKey.F2 => Key.F2, 
			FpsKey.F3 => Key.F3, 
			FpsKey.F4 => Key.F4, 
			FpsKey.F5 => Key.F5, 
			FpsKey.F6 => Key.F6, 
			FpsKey.F7 => Key.F7, 
			FpsKey.F8 => Key.F8, 
			FpsKey.F9 => Key.F9, 
			FpsKey.F10 => Key.F10, 
			FpsKey.F11 => Key.F11, 
			FpsKey.F12 => Key.F12, 
			FpsKey.Escape => Key.Escape, 
			FpsKey.Space => Key.Space, 
			FpsKey.Tab => Key.Tab, 
			FpsKey.Back => Key.Backspace, 
			FpsKey.Return => Key.Enter, 
			FpsKey.LeftShift => Key.LeftShift, 
			FpsKey.RightShift => Key.RightShift, 
			FpsKey.LeftCtrl => Key.LeftCtrl, 
			FpsKey.RightCtrl => Key.RightCtrl, 
			FpsKey.LeftAlt => Key.LeftAlt, 
			FpsKey.RightAlt => Key.RightAlt, 
			FpsKey.Left => Key.LeftArrow, 
			FpsKey.Right => Key.RightArrow, 
			FpsKey.Up => Key.UpArrow, 
			FpsKey.Down => Key.DownArrow, 
			_ => Key.None, 
		};
	}
}
