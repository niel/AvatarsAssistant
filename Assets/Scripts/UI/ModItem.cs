using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class ModItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[Serializable]
		public class ModDef
		{
			public int _id;
			public int _modId;
			public string _creator;
			public string _title;
			public string _desc;
			public string _version;
			public string _url;
			public string _deps;
			public int _icon;
			public int _log;
			public int _clean;
			public string _folder;
			public string _file;
			public bool _installed;
			public bool _isdep;
			public bool _enabled;
		}

		public ModDef moddef;
		public GameObject button_install;
		public GameObject button_update;
		public GameObject button_remove;
		public GameObject button_disable;
		public Text _status;
		public Text _version;
		public Text _desc;
		private ModManager modmanager;

		private void Start()
		{
			modmanager = GameObject.Find("MODManagerCanvas").GetComponent<ModManager>();
			rect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 100, 600, 25);
			foreach (Transform eachChild in transform)
			{
				if (eachChild.name == "Creator")
				{
					// _url = ModManager.mods[_modId].url;
				}

				if (eachChild.name == "Title")
				{
					eachChild.GetComponent<Text>().text = moddef._title;
				}

				if (eachChild.name == "Desc")
				{
					eachChild.GetComponent<Text>().text = moddef._desc;
				}

				if (eachChild.name == "Version")
				{
					eachChild.GetComponent<Text>().text = moddef._version;
				}

				if (eachChild.name == "Icon")
				{
					//_icon = ModManager.mods[_modId].icon;
				}

				if (eachChild.name == "Deps")
				{
					// _url = ModManager.mods[_modId].url;
				}

				if (eachChild.name == "Url")
				{
					// _url = ModManager.mods[_modId].url;
				}

				if (eachChild.name == "Changelog")
				{
					// _url = ModManager.mods[_modId].url;
				}

				if (eachChild.name == "CleanInstall")
				{
					// _url = ModManager.mods[_modId].url;
				}
			}

			if (moddef._installed)
			{
				button_install.SetActive(false);
				button_disable.SetActive(true);
				//check if version match before showing update button
				button_update.SetActive(true);
				button_remove.SetActive(true);
			}
			else
			{
				_status.text = moddef._creator;
			}
		}


		private List<string> alltext = new List<string>();
		private int[] allprogress = new int[20];
		//private bool completed2 = false;
		private bool completed = false;
		private string[] message = new string[20];
		private Rect rect;
		private static bool _inUpdate = false;
		private bool doOnce = false;

		public void btnInstall()
		{
			modmanager.ReloadInstalledMods();

//			modmanager.panelInstall.SetActive(true);
			modmanager.downloadStack = new DownloadStack();
			if (moddef._deps.Length > 1)
			{
				if (moddef._deps.Contains(";"))
				{
					string[] splitDeps = moddef._deps.Split(';');
					for (int i = 0; i < splitDeps.Length; i++)
					{
						for (int x = 0; x < modmanager.deps.Length; x++)
						{
							string[] splitDepName = moddef._deps.Split('/');
							if (modmanager.deps[x].url == splitDepName[splitDepName.Length - 1])
							{
								modmanager.downloadStack.deps.Add(splitDeps[i]);
								modmanager.downloadStack.mods.Add(modmanager.deps[x]);
								Debug.Log("Found dependencies, adding: " + modmanager.deps[x].title);
							}
						}
						//modmanager.downloadStack.depsId.Add(moddef._modId);
					}
				}
				else
				{
					for (int x = 0; x < modmanager.deps.Length; x++)
					{
						string[] splitDepName = moddef._deps.Split('/');
						if (modmanager.deps[x].url == splitDepName[splitDepName.Length - 1])
						{
							modmanager.downloadStack.deps.Add(moddef._deps);
							modmanager.downloadStack.mods.Add(modmanager.deps[x]);
							Debug.Log("Found a single dependency, adding: " + modmanager.deps[x].title);
						}
					}
					//modmanager.downloadStack.depsId.Add(moddef._modId);

				}
			}
			if (moddef._isdep)
			{
				modmanager.downloadStack.deps.Add(moddef._url);
				Debug.Log("Button click: " + moddef._modId);
				modmanager.downloadStack.mods.Add(modmanager.deps[moddef._modId - 1]);
			}
			else
			{
				modmanager.downloadStack.deps.Add(moddef._url);
				Debug.Log("Button click: " + moddef._modId);
				modmanager.downloadStack.mods.Add(modmanager.mods[moddef._modId - 1]);
			}

			//modmanager.downloadStack.depsId.Add(moddef._modId);
			completed = false;
			Debug.Log("Total file to download: " + modmanager.downloadStack.deps.Count);
			modmanager.downloadStack.totalphase = modmanager.downloadStack.deps.Count; // * 2 = download + install
			modmanager.ActiveRoutine = StartCoroutine(ModManager.GetModZip(modmanager.downloadStack, modmanager.installedMods, value => completed = value));
			modmanager.downloading = true;
			Debug.Log("Total phase: " + modmanager.downloadStack.totalphase);
		}

		void Update()
		{
			if (modmanager.downloading)
			{
				if (completed == true)
				{
					//modmanager.finishedInstall.interactable = true;
					if (doOnce)
					{
						doOnce = false;
						modmanager.CheckInstalledMods();
					}
				}
			}
		}
		void OnGUI()
		{
			if (modmanager.downloading == true)
			{
				if (_inUpdate == true)
				{
					if (completed)
					{
						GUI.Label(rect, "Updating " + moddef._title + "... Done!");
					}
					else if (!completed)
					{
						GUI.Label(rect, "Updating " + moddef._title + "...");
					}
				}
				if (_inUpdate == false)
				{
					for (int x = 0; x < modmanager.downloadStack.dlphase + 1; x++)
					{
						rect.y = (Screen.height / 2) - 100 + (x * 20);
						GUI.Label(rect, modmanager.downloadStack.message[x]);
					}
				}
			}
		}

		public void btnDisableEnable()
		{
			if (button_disable.GetComponentInChildren<Text>().text == "Disable")
			{
				//move the lua file to hold
				File.Move(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._file, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/disabled/" + moddef._file);
				for (int x = 0; x < modmanager.installedMods.Length; x++)
				{
					if (modmanager.installedMods[x].file == moddef._file)
					{
						modmanager.installedMods[x].enabled = false;
					}
				}
				button_disable.GetComponentInChildren<Text>().text = "Enable";
			}
			else if (button_disable.GetComponentInChildren<Text>().text == "Enable")
			{
				//move the lua file back
				File.Move(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/disabled/" + moddef._file, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._file);
				//change a single variable in the installedmod.cfg to true or false for enable
				for (int x = 0; x < modmanager.installedMods.Length; x++)
				{
					if (modmanager.installedMods[x].file == moddef._file)
					{
						modmanager.installedMods[x].enabled = true;
					}
				}
				button_disable.GetComponentInChildren<Text>().text = "Disable";
			}
			modmanager.SaveInstalledMods();
		}

		/*installedMods = JsonHelper.FromJson<InstalledMod>(@configText);

		Mod[] myArray = downloadStack.mods.ToArray();
		File.WriteAllText(_dataPath + @"/SavedMods/InstalledMods.cfg", JsonHelper.ToJson(myArray));*/

		public void btnUpdate()
		{
			_inUpdate = true;
			//this udpate only the current mod select for update, not dependencies
			button_update.GetComponent<Button>().interactable = false;
//			modmanager.panelInstall.SetActive(true);
			completed = false;
			doOnce = true;
			modmanager.downloading = true;
			modmanager.ActiveRoutine = StartCoroutine(ModManager.UpdateMod(moddef._modId, modmanager.installedMods, value => completed = value));
		}

		public void btnRemove()
		{
			if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/backup/" + moddef._url)) { File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/backup/" + moddef._url); }
			if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/disabled/" + moddef._file)) { File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/disabled/" + moddef._file); }
			if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._file)) { File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._file); }
			if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._folder)) { Directory.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/Lua/" + moddef._folder, true); }

			List<Mod> newmodlist = new List<Mod>();
			for (int x = 0; x < modmanager.installedMods.Length; x++)
			{
				if (moddef._title == modmanager.installedMods[x].title) { continue; }
				Mod test = new Mod();
				test.creator = modmanager.installedMods[x].creator;
				test.title = modmanager.installedMods[x].title;
				test.desc = modmanager.installedMods[x].desc;
				test.version = modmanager.installedMods[x].version;
				test.deps = modmanager.installedMods[x].deps;
				test.isdep = modmanager.installedMods[x].isDep;
				test.icon = modmanager.installedMods[x].icon;
				test.log = modmanager.installedMods[x].log;
				test.folder = modmanager.installedMods[x].folder;
				test.file = modmanager.installedMods[x].file;
				test.archive = modmanager.installedMods[x].archive;
				test.enabled = modmanager.installedMods[x].enabled;
				test.id = modmanager.installedMods[x].id;
				newmodlist.Add(test);
			}
			Mod[] myArray = newmodlist.ToArray();

			File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/Portalarium/Shroud of the Avatar/SavedMods/InstalledMods.cfg", JsonHelper.ToJson(myArray));
			//modmanager.SaveInstalledMods();
			//downloadStack.mods.Remove(downloadStack.mods[y]);
			modmanager.CheckInstalledMods();
			Destroy(this.gameObject);
		}

		public void OnPointerExit(PointerEventData eventData)
		{

			if (moddef._isdep)
			{
				if (moddef._id % 2 == 0) this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(0f, 0.6f, 1f, 0.2f);
				if (moddef._id % 2 == 1) this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(0f, 0.6f, 1f, 0.1f);
			}
			else
			{
				if (moddef._id % 2 == 0) this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
				if (moddef._id % 2 == 1) this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (moddef._isdep)
			{
				this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(0f, 0.6f, 1f, 0.3f);
			}
			else
			{
				this.gameObject.transform.Find("ColorBG").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
			}
		}
	}
}
