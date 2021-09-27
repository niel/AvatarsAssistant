//
// Credit and thanks go to @DevilCult, for creating the original Mod Manager and shroudmods.com
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
		public List<Mod> modsList; // List bound to ListView for displaying the others (installedModsList, mods, deps, etc.).

		private ListView _listViewContent;

		public VisualTreeAsset modItemTemplate;

		[HideInInspector]
		public Coroutine ActiveRoutine;

		[HideInInspector]
		public bool downloading = false;

		[HideInInspector]
		public DownloadStack downloadStack;

		// ReSharper disable once InconsistentNaming
		private VisualElement _rootVE;

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

			Debug.Log("Fetching list of mods from shroudmods.com.");
			ActiveRoutine = StartCoroutine(FetchModList());
		}

		public void CheckInstalledMods()
		{
			// We get the list of mods from the config file
			string configText = File.ReadAllText(_dataPath + @"SavedMods/InstalledMods.cfg");

			// Destroy any previously created objects, before populating it again.
			ClearModList();

			// If there any mods found, we populate the list with them.
			if (configText.Length > 0)
			{
				installedMods = JsonHelper.FromJson<InstalledMod>(@configText);
				var count = installedMods.Length;
				Debug.Log("CheckInstalledMods - count: " + count);

				modsList.Clear();
				modsList.AddRange(installedMods);
				modsList.Sort();
				Debug.Log("CheckInstalledMods - modsList: " + modsList.Count);
			}
			else // If the file is less than 1, there are no mods found!
			{
				// TODO
				Debug.Log("No Configuration in file.");
			}
		}

		public void CheckScrollViewContentList(Array mods)
		{
			if (mods.Length > 0)
			{ // Regardless of what the list represents, it has entries so the ScrollView must be prepared.
				var AppWindow = _rootVE.Q<VisualElement>("Background");

				if (AppWindow.Q<ScrollView>("ScrollView-Container") == null)
				{
					var ListContainer = AppWindow.Q<VisualElement>("ListContainer");
					var EmptyList     = AppWindow.Q<VisualElement>("EmptyList");
					if (EmptyList != null)
					{
						//ListContainer.Remove(EmptyList);
						EmptyList.visible = false;
					}
					else
					{ // Really! How did the empty list text get removed without ScrollView being created?
						Debug.Log("Really! How did the empty list text get removed without ScrollView being created?");
					}

					ListContainer.Add(new ScrollViewContent());
					_listViewContent = ListContainer.Q<ListView>("ScrollView-Container");
					Debug.Log("ScrollView added");
				}
			}
		}

		private void ClearModList()
		{
			foreach (GameObject obj in _listedModInstance)
			{
				Destroy(obj);
			}

			_listedModInstance.Clear();
			Debug.Log("ClearModList: _listedModInstance count: " + _listedModInstance.Count);
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

		private IEnumerator FetchModList(bool getDependencies = false)
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
					Debug.Log("Mods: " + _jsonString);

					mods = JsonHelper.FromJson<Mod>(_jsonString);
					Debug.Log("Mods[]: " + mods.Length);
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
					Debug.Log("Deps: " + _jsonString);

					if (_jsonString != null)
					{
						deps = JsonHelper.FromJson<Mod>(@_jsonString);
						Debug.Log("Deps[]: " + deps.Length);
						//PopulateModsList(getDependencies);
					}

					break;
			}

			yield return new WaitForSeconds(5);
			_alreadyRefreshing = false;
		}

		public static IEnumerator GetModZip(DownloadStack dlstack, InstalledMod[] _installedmods, Action<bool> completed)
		{
			throw new NotImplementedException("GetModZip");
		}

		private void ListViewAddItem(InstalledMod moditem)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// OnEnable is called when the object becomes enabled and active.
		/// </summary>
		private void OnEnable()
		{
			_rootVE = GetComponent<UIDocument>().rootVisualElement;
			Debug.Log("rooVE count: " +_rootVE.childCount);

			#region Configure files and paths.
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

			/*
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
			*/

			// Check if the installed mods config file is there, if not we create it.
			if (!File.Exists(_dataPath + @"SavedMods/InstalledMods.cfg"))
			{
				File.CreateText(_dataPath + @"SavedMods/InstalledMods.cfg");
			}
			#endregion

			#region Initialise elements for easy access.
			_columnHeaderInstalled = _rootVE.Q<Label>("ColumnHeader-Installed");
    		_columnHeaderLatest    = _rootVE.Q<Label>("ColumnHeader-Latest");
			_emptyList             = _rootVE.Q<Label>("EmptyList");
			_listSwitcher          = _rootVE.Q<Button>("Button-ModsListSwitch");
			_startLauncher         = _rootVE.Q<Button>("Button-StartLauncher");
			_startSotA             = _rootVE.Q<Button>("Button-StartSotA");
			_listViewContent       = _rootVE.Q<ListView>("ListView-Content");

			_startLauncher.visible = false;
			_startSotA.visible     = false;

			_listSwitcher.RegisterCallback<ClickEvent>(ev => SwitchListClicked());

			// TODO add detection code for location of Launcher/SotA Binary. Enable buttons and callbacks if found.
			//m_StartLauncher.RegisterCallback<ClickEvent>(ev => StartLauncherClicked());
			//m_StartSotA.RegisterCallback<ClickEvent>(ev => StartSotAClicked());
			#endregion

			#region Prepare the ListVew element.
			var modItem = Resources.Load<VisualTreeAsset>("UI/ModItem");
			// The "makeItem" function is called when the ListView needs more items to render.
			Func<VisualElement> makeItem = () => modItem.Instantiate();

			// As the user scrolls through the list, the ListView object recycles elements created by the "makeItem"
			// function, and invokes the "bindItem" callback to associate the element with the matching data item
			// (specified as an index in the list).
			Action<VisualElement, int> bindItem = (e, i) =>
												  {
													  var mod       = modsList[i];
													  var title     = e.Q<Label>("Name");
													  var desc      = e.Q<Label>("Description");
													  var installed = e.Q<Label>("Installed");
													  var latest    = e.Q<Label>("Latest");
													  var enable    = e.Q<Button>("Enabled");
													  var update    = e.Q<Button>("Update");
													  var remove    = e.Q<Button>("Remove");

													  title.text     = mod.title;
													  desc.text      = mod.desc;
													  installed.text = mod.version;
													  latest.text    = "";
													  //enable.RegisterCallback();
													  //update.RegisterCallback();
													  //remove.RegisterCallback();

												  };

			var listContainer = _rootVE.Q<VisualElement>("ListContainer");

			_listViewContent                               = new ListView(modsList, 75.0f, makeItem, bindItem);
			_listViewContent.AddToClassList("listView");
			//_listViewContent.bindItem                      = bindItem;
			//_listViewContent.fixedItemHeight               = 20.0f;
			//_listViewContent.itemsSource                   = modList;
			//_listViewContent.makeItem                      = makeItem;
			_listViewContent.selectionType                 = SelectionType.None;
			_listViewContent.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;

			listContainer.Add(_listViewContent);
			#endregion
			Debug.Log("OnEnable - ListView: " + _listViewContent.childCount);

			// Set the mode to Mods Available, then immediately flip it to cause the contents panel to update for Installed mods.
			_listMode = Mods.Available;
			SwitchListClicked();
		}

		private void PopulateListView(bool getDependencies)
		{
			// TODO set the column headers for current/installed to Installed/Latest
			switch (_listMode)
			{
				case Mods.Available when getDependencies:
				{
					if (deps.Length > 0)
					{
						Debug.Log("Populating Dependencies!");
						// TODO
						//CheckScrollViewContentList(deps);
						throw new NotImplementedException();
					}

					return;
				}
				case Mods.Available:
				{
					_columnHeaderInstalled.text = "Creator";
					if (mods.Length > 0)
					{
						Debug.Log("Populating Available Mods!");
						//CheckScrollViewContentList(mods);

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

							if (!found)
							{
								;
							}

							Debug.Log("PopulateScrollView: Adding new item...");
							ScrollViewAddItem(mods[i]);
						}
					}

					break;
				}
				case Mods.Installed:
				{
					Debug.Log("PopulateListView - count: " + modsList.Count);
					_columnHeaderLatest.text = "Latest";
					if (modsList.Count > 0)
					{
						Debug.Log("PopulateListView: Installed Mods");
						//CheckScrollViewContentList(installedMods); // Create ScrollView-Container if it doesn't exist yet.

/*
						foreach (InstalledMod moditem in installedMods)
						{
							//ListViewAddItem(moditem);
						}
*/
					}
					else
					{
						// TODO
						throw new NotImplementedException("PopulateListView: Not processing zero installed mods yet!");

					}
					break;
				}
				default:
					;

					break;
			}
		}

		public void PopulateModsList(bool getDependencies = false)
		{
			// First we have to get our list of Mods. How we do that depends on the list we are interested in.
			switch (_listMode)
			{
				case Mods.Installed:
				{
					CheckInstalledMods();

					//Debug.Log("PopulateModsList - ModsList: " + modsList.Count);
					if (modsList.Count > 0)
					{
						//PopulateListView(false);
					}
					else
					{
						// TODO create the empty list conditions. Might be background image, might be a NoItemsObject, etc.
						//Debug.Log("_emptyList.visible: " + _emptyList);
						//_emptyList.visible = true;
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

			_listViewContent.Rebuild();
			Debug.Log("PopulateModsList - ListView: " + _listViewContent.childCount);

			//PopulateListView(getDependencies);
		}

		public void PrintModsList()
		{
			if (mods.Length > 0)
			{
				foreach (Mod moditem in mods)
				{
					Debug.Log("Name: " + moditem.title + "\n");
				}
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
			//Debug.Log("ScrollViewAddItem: entered");
/*
			var modItemUI = modItemTemplate.Instantiate();

			var lblName          = modItemUI.Q<Label>("Name");
			var lblDesc          = modItemUI.Q<Label>("Description");
			var lblInstalled     = modItemUI.Q<Label>("Installed");
			var lblLatest        = modItemUI.Q<Label>("Latest");
			var btnEnableDisable = modItemUI.Q<Button>("ButtonEnableDisable");
			var btnUpdate        = modItemUI.Q<Button>("ButtonUpdate");
			var btnRemove        = modItemUI.Q<Button>("ButtonRemove");

			lblName.text      = modItem.title;
			lblDesc.text      = modItem.desc;
			lblInstalled.text = _listMode == Mods.Available ? modItem.creator : modItem.version;  // Installed version or creator
			lblLatest.text    = _listMode == Mods.Available ? modItem.version : "somehow get latest";  // Newest
*/
			var modItemUI = new ModListEntry(modItem);

			// TODO determine which buttons should be enabled/disabled, grey out if disabled - add ClickEvent if enabled.
			//btnRemove.clicked += () => modItemUI.RemoveFromHierarchy();

			_listViewContent.Add(modItemUI);
			Debug.Log("ScrollViewAddItem: Added " + modItem.title);
			Debug.Log("_scrollViewContent: " + _listViewContent.childCount);
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

			PopulateModsList(false);
		}

		private void ShowChildren(VisualElement element)
		{
			if (element.childCount > 0)
			{
				foreach (var child in element.Children())
				{
					Debug.Log("Child: " + child.name);
					ShowChildren(child);
				}
			}
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
