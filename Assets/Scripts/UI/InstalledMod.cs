using System;

namespace UI
{
	/// <summary>
	/// Represents a Mod that has been installed locally.
	/// </summary>
	[Serializable]
	public class InstalledMod : Mod
	{
			public InstalledMod()
			{
				clean = -1; // Just setting unused value to indicate it's not part of general use for this class.
			}
	}
}
