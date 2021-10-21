using System;
using System.Collections.Generic;
using AA.UI.Mods;
using UnityEngine;

namespace AA.Web
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

		public void Add(string itemDep, Mod managerDep, Mod item)
		{
			var splitName = item.deps.Split('/');
			if (managerDep.url == splitName[splitName.Length - 1])
			{
				deps.Add(itemDep);
				mods.Add(managerDep);
				Debug.Log("ModItem '" + item.title + "' found dependency, adding: " + managerDep.title);
			}
			else
			{
				Debug.Log("ModItem Install: Cannot find dependency: " + itemDep);
			}
		}

		public void SendMessage(string messageToSend, int index = 0)
		{
			if (index > 0)
			{
				if (this.message[index] != messageToSend)
				{
					this.message[index] = messageToSend;

					//Debug.Log("Download stack message: " + messageToSend);
				}
			}
			else if (this.message.Count == 0 || this.message[this.message.Count - 1] != messageToSend)
			{
				this.message.Add(messageToSend);

				//Debug.Log("Download stack message: " + messageToSend);
			}
		}
	}
}
