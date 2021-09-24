using System;

namespace UI
{
	// Mods fetched from the web
	[Serializable]
	public class Mod
	{
		public int    id;
		public string creator;
		public string title;
		public string desc;
		public string version;
		public string url;
		public string deps;
		public bool   isdep;
		public int    icon;
		public int    log;
		public int    clean;
		public string folder;
		public string file;
		public string backupzip;
		public bool   enabled;
	}
}
