using System;

namespace UI
{
	/// <summary>
	/// Represents a Mod listed on shroudmods.com
	/// </summary>
	[Serializable]
	public class Mod : IComparable
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
		public string latest = "";

		public int CompareTo(object mod)
		{
			var item = (Mod) mod;
			if (item.title == null)
			{
				return 1;
			}
			else
			{
				return this.title.CompareTo(item.title);
			}
		}

		public int SortByNameAscending(string title1, string title2)
		{
			return title1.CompareTo(title2);
		}
	}
}
