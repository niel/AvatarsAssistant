using System;

namespace UI
{
	// Mods fetched from the web
	[Serializable]
	public class Mod
	{
		public int    id;
		public string creator = null;
		public string title;
		public string version;
		public string desc;
		public string url;
		public string deps;
		public bool   isdep;
		public int    icon;
		public int    log;
		public int    clean;
		public string folder;
		public string file;
		public string archive;
		public bool   enabled;
	}
}
