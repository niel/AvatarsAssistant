using System;
using System.Collections.Generic;

namespace SotaAssistant.UI.Mods
{
	[Serializable]
	public class DownloadStack
	{
		public List<Mod>    mods       = new List<Mod>();
		public List<int>    depsId     = new List<int>();
		public List<string> deps       = new List<string>();
		public List<string> message    = new List<string>();
		public int          dlphase    = 0;
		public int          totalphase = 0;
	}
}
