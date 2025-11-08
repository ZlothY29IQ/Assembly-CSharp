using System;

[Flags]
public enum CrittersBiome
{
	Forest = 1,
	Mountain = 2,
	Desert = 4,
	Grassland = 8,
	Cave = 0x10,
	IntroArea = 0x40000000,
	Any = -1
}
