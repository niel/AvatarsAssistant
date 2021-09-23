//
// Most credit and thanks should go to @DevilCult, for creating the original Mod Manager and shroudmods.com
//
// This is a re-implementation of that original ShroudModManager.
// I started re-implementing it because there were a couple of issues I wanted to fix (display of \r\n as characters
// instead of codes) and I thought it would be a good exercise in converting a uGUI interface into a UI Toolkit one in
// Unity.
//
// @author Archer
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace UI
{
	public class ModManager : MonoBehaviour
	{
		const string ModsListUrl = "https://shroudmods.com/";

		private static bool testing = true;

		private bool   _alreadyRefreshing;
		private Label  _columnHeaderInstalled;
		private Label  _columnHeaderLatest;
		private string _dataPath;
		private Label  _emptyList;
		private Button _finishedInstall;
		private string _jsonString;
		private Button _listSwitcher;
		private Button _startLauncher;
		private Button _startSotA;

		private Mods _listMode;

		public Mod[] deps;
		public Mod[] mods;

		private ScrollView _scrollViewContent;

		public VisualTreeAsset modItemTemplate;

		[HideInInspector]
		public Coroutine ActiveRoutine;

		[HideInInspector]
		public bool downloading = false;

		[HideInInspector]
		public DownloadStack downloadStack;

		// ReSharper disable once InconsistentNaming
		private VisualElement _rootVE;

		// InstalledMods.cfg
		[Serializable]
		public struct InstalledMod
		{
			public int id;
			public string creator;
			public string title;
			public string desc;
			public string version;
			public string deps;
			public bool isDep;
			public int icon;
			public int log;
			public string folder;
			public string file;
			public string archive;
			public bool enabled;
		}

		public VisualElement installedModsScrollviewContent;
		public VisualElement modObject;
		public VisualElement noModFound;
		public VisualElement panelInstall;

		public InstalledMod[] installedMods;

		private List<GameObject> _listedModInstance = new List<GameObject>();


		private void CheckAvailableModList()
		{
			if (_listMode == Mods.Installed)
			{
				throw new InvalidDataException("Incorrect Mode when calling CheckAvailableModList");
			}

			ActiveRoutine = StartCoroutine(GetModList());
		}

		public void CheckInstalledMods()
		{
			// We get the list of mods from the config file
			string configText = File.ReadAllText(_dataPath + @"SavedMods/InstalledMods.cfg");

			// Destroy any previously created objects, before populating it again.
			ClearModList();

			// If there any mods found, we populate the list with them.
			// If there are none the ScrollView has a background showing 'No Mods Found' as a temporary measure.
			if (configText.Length > 0)
			{
//				installedMods = JsonHelper.FromJson<InstalledMod>(@configText);
//				PopulateModList(true, false);
			}
			else // If the file is less than 1, there are no mods found! So we set the scrollview to use the 'No Mods Found' background image.
			{
				// background mage defaults to 'No Mods Found' image. If there are Mods found, listing them covers the image - works for now!

			}
		}

		private void ClearModList()
		{
			foreach (GameObject obj in _listedModInstance)
			{
				Destroy(obj);
			}

			_listedModInstance.Clear();
			//Debug.Log(listedModInstance.Count);
		}

		private void DownLoadModList()
		{
			throw new NotImplementedException();
		}

		private static string fixJson(string value)
		{
			value = "{\"Items\":" + value + "}";
			return value;
		}

		public static IEnumerator GetModZip(DownloadStack dlstack, InstalledMod[] _installedmods, Action<bool> completed)
		{
			throw new NotImplementedException("GetModZip");
		}

		private IEnumerator GetModList()
		{
			if (_alreadyRefreshing)
			{
				yield break;
			}
			else
			{
				_alreadyRefreshing = true;
			}
			ClearModList();

			// Get list of mods first.
			var www = UnityWebRequest.Get(ModsListUrl + "/getmods.php?request=mods");
			www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey(); // Use this if you have any problem with a certificate, need to set the public key into AcceptAllCertificatesSignedWithASpecificiedPublicKey.cs script

			yield return www.SendWebRequest();

			switch (www.result)
			{
				case UnityWebRequest.Result.ConnectionError:
					Debug.Log("Network Error: " + www.responseCode + " " + www.error);

					break;
				case UnityWebRequest.Result.ProtocolError:
					Debug.Log("HTTP Error: " + www.responseCode + " " + www.error);

					break;
				default:
					_jsonString = fixJson(www.downloadHandler.text);
					Debug.Log(_jsonString);

					mods = JsonHelper.FromJson<Mod>(_jsonString);
					PopulateModsList(false);

					break;
			}

			// Now we get the dependencies.
			www                    = UnityWebRequest.Get(ModsListUrl + "/getmods.php?request=deps");
			www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

			yield return www.SendWebRequest();
			switch (www.result)
			{
				case UnityWebRequest.Result.ConnectionError:
					Debug.Log("Network Error: " + www.responseCode + " " + www.error);

					break;
				case UnityWebRequest.Result.ProtocolError:
					Debug.Log("HTTP Error: " + www.responseCode + " " + www.error);

					break;
				default:
					_jsonString = fixJson(www.downloadHandler.text);
					Debug.Log(_jsonString);

					if (_jsonString != null)
					{
						deps = JsonHelper.FromJson<Mod>(@_jsonString);
						PopulateModsList(true);
					}

					break;
			}

			yield return new WaitForSeconds(5);
			_alreadyRefreshing = false;
		}

		/// <summary>
		/// OnEnable is called when the object becomes enabled and active.
		/// </summary>
		private void OnEnable()
		{
			_rootVE = GetComponent<UIDocument>().rootVisualElement;

			string sotaDirectory = testing
									   ? @"/Portalarium/Shroud of the Avatar(QA)/"
									   : @"/Portalarium/Shroud of the Avatar/";
			string baseAppInstallLocation;

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.LinuxPlayer:
				case RuntimePlatform.OSXPlayer:
					baseAppInstallLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

					break;
				default:
					baseAppInstallLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

					break;
			}
			_dataPath = baseAppInstallLocation + sotaDirectory;

			// Check that necessary directories and files exist.
			if (!Directory.Exists(_dataPath + @"SavedMods"))
			{
				Directory.CreateDirectory(_dataPath + @"SavedMods");
			}

			if (!Directory.Exists(_dataPath + @"SavedMods/backup"))
			{
				Directory.CreateDirectory(_dataPath + @"SavedMods/backup");
			}

			if (!Directory.Exists(_dataPath + @"SavedMods/disabled"))
			{
				Directory.CreateDirectory(_dataPath + @"SavedMods/disabled");
			}

			// Check if our settings file exists. if not, create it (this only saves the location of the launcher)
			if (!File.Exists(_dataPath + @"SavedMods/Settings.cfg"))
			{
				//File.CreateText(_dataPath + @"/SavedMods/Settings.cfg");
				//create only when the button is clicked
			}
			else
			{
				JsonUtility.FromJson<LauncherLocation>(File.ReadAllText(_dataPath + @"SavedMods/Settings.cfg"));
			}

			//Debug.Log("Checking existence of file: " + _dataPath + @"SavedMods/InstalledMods.cfg");
			// Check if the installed mods config file is there, if not we create it.
			if (!File.Exists(_dataPath + @"SavedMods/InstalledMods.cfg"))
			{
				//Debug.Log("Creating InstalledMods file!");
				File.CreateText(_dataPath + @"SavedMods/InstalledMods.cfg");
				//listedModInstance.Add((GameObject)Instantiate(NoModFound, installedModsScrollviewContent.transform));
			}
			else // if the file exists we check if there are any installed mods.
			{
				//Debug.Log("Checking for installed mods!");
				//CheckInstalledMods();
			}
			//Debug.Log("About to set List Mode");

			_columnHeaderInstalled = _rootVE.Q<Label>("ColumnHeader-Installed");
    		_columnHeaderLatest    = _rootVE.Q<Label>("ColumnHeader-Latest");
			_emptyList             = _rootVE.Q<Label>("EmptyList");
			_listSwitcher          = _rootVE.Q<Button>("Button-ModsListSwitch");
			_startLauncher         = _rootVE.Q<Button>("Button-StartLauncher");
			_startSotA             = _rootVE.Q<Button>("Button-StartSotA");
			_scrollViewContent     = _rootVE.Q<ScrollView>("ScrollView-Content");

			modItemTemplate = Resources.Load<VisualTreeAsset>("UI/ModItem");

			_startLauncher.visible = false;
			_startSotA.visible = false;

			_listSwitcher.RegisterCallback<ClickEvent>(ev => SwitchListClicked());

			/* TODO add detection code for location of Launcher/SotA Binary. Enable buttons and callbacks if found.
			m_StartLauncher.RegisterCallback<ClickEvent>(ev => StartLauncherClicked());
			m_StartSotA.RegisterCallback<ClickEvent>(ev => StartSotAClicked());
			*/

			// Set the mode to Mods Available, then immediately flip it to cause the contents panel to update for Installed mods.
			_listMode = Mods.Available;
			SwitchListClicked();
		}

		private void PopulateModsList(bool getDependencies = false)
		{
			_rootVE = GetComponent<UIDocument>().rootVisualElement;

			// First we have to get our list of Mods. How we do that depends on the list we are interested in.
			switch (_listMode)
			{
				case Mods.Installed:
				{
					// TODO set the column headers for current/installed to Installed/Latest
					CheckInstalledMods();

					if (installedMods.Length > 0)
					{
						//_emptyList.visible = true;
						// TODO actually process the config file for the scrollview to use below.
					}
					else
					{
						_emptyList.visible = true;
					}

					break;
				}
				case Mods.Available:
					// TODO Set the column headers for current/installed to Latest/Creator
					CheckAvailableModList();

					break;
				default:
					throw new ArgumentException("Attempt to populate list from incorrect mode!");
					break;
			}

			PopulateScrollView(getDependencies);
		}

		private void PopulateScrollView(bool getDependencies)
		{
			if (mods.Length > 0)
			{
				if (_listMode == Mods.Available)
				{
					Debug.Log("Populating list of mods from shroudmods.com.");
					if (getDependencies)
					{
						if (deps.Length > 0)
						{
							// TODO
							throw new NotImplementedException();
						}

						return;
					}

					if (mods.Length > 0)
					{
						for (int i = 0; i < mods.Length; i++)
						{
							bool found = false;
							if (installedMods != null)
							{
								for (int j = 0; j < installedMods.Length; j++)
								{
									if (mods[i].title  == installedMods[j].title  ||
										mods[i].folder == installedMods[j].folder ||
										mods[i].file   == installedMods[j].file)
									{
										found = true;

										break; // break out of inner (j) loop to the next mode.
									}
								}
							}

							if (found)
							{
								continue;
							}

							ScrollViewAddItem(mods[i]);
						}
					}
				}
				else
				{
					// TODO
					throw new NotImplementedException("PopulateScrollView: Not processing Installed mods yet!");
				}
			}
			else
			{
				;
			}
		}

		public void ReloadInstalledMods()
		{
			throw new NotImplementedException();
		}

		public void SaveInstalledMods()
		{
			// Mod[] myArray = installedMods.ToArray();
			File.WriteAllText(_dataPath + @"/SavedMods/InstalledMods.cfg", JsonHelper.ToJson(installedMods));
		}

		private void ScrollViewAddItem(Mod modItem)
		{
			var modItemUI = modItemTemplate.Instantiate();

			var lblName          = modItemUI.Q<Label>("Name");
			var lblDesc          = modItemUI.Q<Label>("Description");
			var lblInstalled     = modItemUI.Q<Label>("Installed");
			var lblLatest        = modItemUI.Q<Label>("Latest");
			var btnEnableDisable = modItemUI.Q<Button>("ButtonEnableDisable");
			var btnUpdate        = modItemUI.Q<Button>("");
			var btnRemove        = modItemUI.Q<Button>("");

			lblName.text      = modItem.title;
			lblDesc.text      = modItem.desc;
			lblInstalled.text = _listMode == Mods.Available ? modItem.creator : modItem.version;
			lblLatest.text    = modItem.version;

			//itemIdLbl.text = System.Guid.NewGuid().ToString();
			//btnRemove.clicked += () => modItemUI.RemoveFromHierarchy();

			_scrollViewContent.Add(modItemUI);
		}

		private void SwitchListClicked()
		{
			switch (_listMode)
			{
				case Mods.Installed:
					_listMode          = Mods.Available;
					_listSwitcher.text = "Installed Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderLatest.text    = "Creator";

					break;
				case Mods.Available:
					_listMode          = Mods.Installed;
					_listSwitcher.text = "Available Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderLatest.text    = "Newest";

					break;
				default:
					// WTF, Should never be set to Disabled!!
					break;
			}
			//Debug.Log("List Mode set!");

			PopulateModsList(true);
		}

		private void StartLauncherClicked()
        {
			throw new NotImplementedException();
			if (_startLauncher.visible)
            {
				;
			}
		}

        private void StartSotAClicked()
        {
			throw new NotImplementedException();
			if (_startSotA.visible)
			{
				;
			}

		}

		public static IEnumerator UpdateMod(int id, InstalledMod[] _installedmods, Action<bool> completed)
		{
			throw new NotImplementedException();
			yield break;
		}
	}
}
