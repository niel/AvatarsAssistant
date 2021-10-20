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
using System.IO.Compression;
using System.Linq;
using AA.Utility;
using AA.Web;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

namespace AA.UI.Mods
{
	public class Manager : MonoBehaviour
	{
		#region constants and variables

		private const string NothingFoundJson =
			@"{""Items"":[{""id"": 0,""creator"": ""Archer"",""title"": ""NoModsFound"",""desc"": ""Dummy entry for empty list"",""version"": ""1.0"",""url"": "" "",""deps"": "" "",""isdep"": false,""icon"": 1,""log"": 1,""clean"": 0,""folder"": ""dummy"",""file"": ""dummy.lua"",""backupzip"": "" "",""enabled"": false}]}";

		private const string WebSiteUrl = "https://shroudmods.com/";

		private static bool   _alreadyDownloading;
		private        bool   _alreadyRefreshing;
		private        Label  _columnHeaderInstalled;
		private        Label  _columnHeaderLatest;
		private        bool   _doOnce;
		private        Button _finishedInstall;
		private        string _jsonString;
		private        Button _listSwitcher;
		private        Button _startLauncher;
		private        Button _startSotA;

		private  VisualElement _listContainer;
		internal Mods          listMode;

		public Mod[] deps;
		public Mod[] mods;

		// List bound to ListView for displaying the others (installedModsList, mods, deps, etc.).
		public List<Mod> modsList;

		private ListView _listViewContent;

		public VisualTreeAsset modItemTemplate;

		private Coroutine _activeRoutine;

		[HideInInspector] public bool downloading;

		// ReSharper disable once InconsistentNaming
		private VisualElement _rootVE;

		public static InstalledMod[] installedMods;

		private string _versions;
		private bool   _fetchingVersions;

		#endregion

		private void CheckAvailableModList()
		{
			if (listMode == Mods.Installed)
			{
				throw new InvalidDataException("Incorrect Mode when calling CheckAvailableModList");
			}

			// Clear list.
			modsList.Clear();

			//Debug.Log("CheckAvailableMods: modsList count: " + modsList.Count);

			//Debug.Log("Fetching list of mods from shroudmods.com.");
			_activeRoutine = StartCoroutine(FetchModList());
		}

		public void CheckInstalledMods()
		{
			// We get the list of mods from the config file
			string configText = File.ReadAllText(@Main.ModsInstalledFile);

			// Destroy any previously created objects, before populating it again.
			modsList.Clear();

			// If there are any mods found, we populate the list with them.
			if (configText.Length > 0)
			{
				installedMods = JsonHelper.FromJson<InstalledMod>(@configText);

				modsList.AddRange(installedMods);
				modsList.Sort();

				_versions = "";
				for (int i = 0; i < installedMods.Length; i++)
				{
					_versions = _versions + ";" + installedMods[i].id;
				}

				_versions = _versions[1..];

				_activeRoutine = StartCoroutine(CheckVersions(installedMods.Length));
			}
			else // If the file is less than 1, there are no mods found!
			{
				//throw new NotImplementedException("CheckInstalledMods: Empty Config File");
				Debug.Log("CheckInstalledMods: No Configuration in file.");

				installedMods = JsonHelper.FromJson<InstalledMod>(@NothingFoundJson);
				modsList.AddRange(installedMods);
			}
		}

		IEnumerator CheckVersions(int count)
		{
			if (_fetchingVersions)
			{
				yield break;
			}
			else
			{
				_fetchingVersions = true;
			}

			string[] versions = new string[count];

			if (!string.IsNullOrEmpty(_versions))
			{
				versions = _versions.Split(';');

				UnityWebRequest www = UnityWebRequest.Get(WebSiteUrl + "getmods.php?request=;" + _versions);
				www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
				UnityWebRequestAsyncOperation getting = www.SendWebRequest();

				while (!getting.isDone)
				{
					yield return null;
				}

				switch (www.result)
				{
					case UnityWebRequest.Result.ConnectionError:
						Debug.Log("Network Error: " + www.responseCode + " " + www.error);
						_fetchingVersions = false;

						yield break;
					case UnityWebRequest.Result.ProtocolError:
						Debug.Log("HTTP Error: " + www.responseCode + " " + www.error);
						_fetchingVersions = false;

						yield break;
					default:
						string jsonString = FixJson(www.downloadHandler.text);

						if (jsonString != null)
						{
							versions = JsonHelper.FromJson<string>(@jsonString);
						}

						break;
				}
			}
			else
			{
				Debug.Log("CheckVersions: Not sending request as string is null or empty!");
			}

			for (int i = 0; i < installedMods.Length; i++)
			{
				installedMods[i].latest = versions[i];
			}

			_listViewContent.Rebuild();
		}

		private void DownLoadModList()
		{
			throw new NotImplementedException();
		}

		private static string FixJson(string value)
		{
			value = "{\"Items\":" + value + "}";

			return value;
		}

		public static IEnumerator FetchModArchive(DownloadStack stack, Action completed)
		{
			if (_alreadyDownloading)
			{
				yield break;
			}
			else
			{
				_alreadyDownloading = true;
			}

			Debug.Log("FetchModArchive: Entered!");

			UnityWebRequestAsyncOperation download;
			UnityWebRequest               www;

			// Download everything from the stack (dependencies to mod)

			if (stack.deps != null)
			{
				for (int i = 0; i < stack.totalphase; i++)
				{
					www                    = UnityWebRequest.Get(WebSiteUrl + "mods/" + stack.deps[i]);
					www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
					download               = www.SendWebRequest();
					stack.SendMessage("Download stack: Checking next step...");
					while (!download.isDone)
					{
						if (i == stack.deps.Count - 1)
						{
							stack.SendMessage("Downloading " + stack.deps[i] + " " + Mathf.RoundToInt(download.progress * 100) + "%",
											  stack.dlphase);
						}
						else
						{
							stack.SendMessage("Downloading dependency " + stack.deps[i] + " " + Mathf.RoundToInt(download.progress * 100) + "%",
											  stack.dlphase);
						}

						yield return null;
					}

					switch (www.result)
					{
						case UnityWebRequest.Result.ConnectionError:
							Debug.Log("Network Error: " + www.responseCode + " " + www.error);

							break;
						case UnityWebRequest.Result.ProtocolError:
							Debug.Log("HTTP Error: " + www.responseCode + " " + www.error);

							break;
						default:
							Debug.Log("FetchModArchive saving downloaded file: " + stack.deps[i]);
							File.WriteAllBytes(Main.ModsSavedBackupPath + stack.deps[i], www.downloadHandler.data);

							if (stack.deps.Count - 1 == i)
							{
								Debug.Log("FetchModArchive: count = "   + stack.deps.Count);
								Debug.Log("FetchModsArchive : index = " + i);

								//Debug.Log("    Mods: " + stack.mods[1].title);
								stack.SendMessage("Downloading " + stack.deps[i] + " 100%", stack.dlphase);

								// Install: extract to Lua directory. HUH! how does that work?
								stack.mods[i].enabled = false;

								//Debug.Log("FetchModArchive: Dunno why, but it's always out of range, so skipped!");
							}
							else
							{
								Debug.Log("false");
								stack.SendMessage("Downloading dependency " + stack.deps[i] + " 100%", stack.dlphase);

								// Install: extract to Lua directory. HUH! how does that work?
								stack.mods[i].enabled = false;
							}

							// Extract
							Debug.Log("FetchModArchive: extracting the archive: " + Main.ModsSavedBackupPath +
									  stack.deps[i]);
							using (ZipArchive archive =
								ZipFile.Open(Main.ModsSavedBackupPath + stack.deps[i], ZipArchiveMode.Update))
							{
								ZipArchiveExtensions.ExtractToDirectory(archive, Main.LuaPath, true);
							}

							stack.mods[i].enabled = true;
							stack.SendMessage("Installing " + stack.deps[i], stack.dlphase);

							break;
					}

					stack.dlphase += 1;
				}

				for (int i = 0; i < installedMods.Length; i++)
				{
					for (int j = 0; j < stack.mods.Count; j++)
					{
						if (installedMods[i].id == stack.mods[j].id)
						{
							Debug.Log("FetchModArchive - Matched mod: " + installedMods[i].title);

							// Remove old mod archive file, check if it is a clean install and if so, remove file from disabled or Lua directory.
							if (File.Exists(Main.ModsSavedBackupPath + stack.mods[j].url))
							{
								File.Delete(Main.LuaPath + stack.mods[j].url);
							}

							if (File.Exists(Main.ModsSavedDisabledPath + stack.mods[j].url))
							{
								File.Delete(Main.ModsSavedDisabledPath + stack.mods[j].url);
							}

							if (stack.mods[j].clean == 1)
							{
								if (File.Exists(Main.LuaPath + stack.mods[j].file))
								{
									File.Delete(Main.LuaPath + stack.mods[j].file);
								}

								if (Directory.Exists(Main.LuaPath + stack.mods[j].folder))
								{
									Directory.Delete(Main.LuaPath + stack.mods[j].folder, true);
								}
							}

							Debug.Log("Attempting to remove mod from the fetching list.");
							stack.mods.Remove((stack.mods[j]));
						}
					}

					//var entry = new Mod();
					Debug.Log("Attempting to add mod to installed mods list.");
					stack.mods.Add(installedMods[i]);
					Debug.Log("SUCCESS!!");
				}

				Mod[] myArray = stack.mods.ToArray();
				File.WriteAllText(Main.ModsInstalledFile, JsonHelper.ToJson(myArray));
				stack.SendMessage("Your mod is now installed!");
			}
			else
			{
				stack.SendMessage("ERROR: The download stack is empty. Something weird happened!!");
			}

			completed();
			_alreadyDownloading = false;
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

			//Debug.Log("Manager - FetchModList: entry");

			// Get list of mods first.
			var www = UnityWebRequest.Get(WebSiteUrl + "/getmods.php?request=mods");
			www.certificateHandler =
				new AcceptAllCertificatesSignedWithASpecificKeyPublicKey(); // Use this if you have any problem with a certificate, need to set the public key into AcceptAllCertificatesSignedWithASpecifiedPublicKey.cs script

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
					_jsonString = FixJson(www.downloadHandler.text);

					//Debug.Log("Manager - FetchModList: Mods: " + _jsonString);

					mods = JsonHelper.FromJson<Mod>(_jsonString);

					//Debug.Log("Manager - FetchModList: Mods[]: " + mods.Length);

					//PopulateModsList(false);
					modsList.AddRange(mods);
					modsList.Sort();

					//Debug.Log("Manager - FetchModList: modsList count: " + modsList.Count);
					_listViewContent.Rebuild();

					break;
			}

			// Now we get the dependencies.
			www                    = UnityWebRequest.Get(WebSiteUrl + "/getmods.php?request=deps");
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
					_jsonString = FixJson(www.downloadHandler.text);

					//Debug.Log("Manager - FetchModList: Deps: " + _jsonString);

					if (_jsonString != null)
					{
						deps = JsonHelper.FromJson<Mod>(@_jsonString);

						//Debug.Log("Manager - FetchModList: Deps[]: " + deps.Length);

						//PopulateModsList(getDependencies);
						modsList.AddRange(deps);
						modsList.Sort();

						//Debug.Log("Manager - FetchModList: modsList count: " + modsList.Count);
						_listViewContent.Rebuild();
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
			_rootVE = GetComponent<UIDocument>().rootVisualElement.Q("ModManager");
			if (_rootVE == null)
			{
				throw new Exception("ModManager - OnEnable: WTF!");
			}

			//Debug.Log("OnEnable - RootVE: " + _rootVE);

			#region Initialise elements for easy access.

			_columnHeaderInstalled = _rootVE.Q<Label>("ColumnHeader-Installed");
			_columnHeaderLatest    = _rootVE.Q<Label>("ColumnHeader-Latest");
			_listSwitcher          = _rootVE.Q<Button>("Button-ModsListSwitch");
			_startLauncher         = _rootVE.Q<Button>("Button-StartLauncher");
			_startSotA             = _rootVE.Q<Button>("Button-StartSotA");
			_listContainer         = _rootVE.Q<VisualElement>("ListContainer");

			_startLauncher.visible = false;
			_startSotA.visible     = false;

			_listSwitcher.RegisterCallback<ClickEvent>(SwitchListClicked, TrickleDown.TrickleDown);

			// TODO add detection code for location of Launcher/SotA Binary. Enable buttons and callbacks if found.
			//m_StartLauncher.RegisterCallback<ClickEvent>(ev => StartLauncherClicked());
			//m_StartSotA.RegisterCallback<ClickEvent>(ev => StartSotAClicked());

			#endregion

			#region Prepare the ListView element.

			// The "makeItem" function is called when the ListView needs more items to render.
			Func<VisualElement> makeItem = () => new ModItem(this);

			// As the user scrolls through the list, the ListView object recycles elements created by the "makeItem"
			// function, and invokes the "bindItem" callback to associate the element with the matching data item
			// (specified as an index in the list).
			// ReSharper disable once ConvertToLocalFunction
			// ReSharper disable once PossibleNullReferenceException
			Action<VisualElement, int> bindItem = (e, i) => (e as ModItem).BindEntry(modsList[i]);

			_listViewContent = new ListView(modsList, 75.0f, makeItem, bindItem)
							   {
								   name                          = "ListViewContent",
								   selectionType                 = SelectionType.None,
								   showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly
							   };
			_listViewContent.AddToClassList("listview");

			_listContainer.Add(_listViewContent);

			#endregion

			// Set the mode to Mods Available, then immediately flip it to cause the contents panel to update for Installed mods.
			listMode = Mods.Available;
			SwitchListClicked();
		}

		private void PopulateModsList()
		{
			// First we have to get our list of Mods. How we do that depends on the list we are interested in.
			switch (listMode)
			{
				case Mods.Installed:
				{
					// TODO Set the column headers for current/installed to Latest/Creator
					CheckInstalledMods();

					break;
				}
				case Mods.Available:
					// TODO Set the column headers for current/installed to Latest/Creator
					CheckAvailableModList();

					break;
				default:
					throw new ArgumentException("Attempt to populate list from incorrect mode!");
			}

			_listViewContent.Rebuild();
		}

		public void PrintModsList()
		{
			if (mods.Length > 0)
			{
				foreach (var modItem in mods)
				{
					Debug.Log("Name: " + modItem.title + "\n");
				}
			}
		}

		public void ReloadInstalledMods()
		{
			throw new NotImplementedException();
		}

		public void RemoveMod(Mod mod)
		{
			if (File.Exists(Main.ModsSavedBackupPath + mod.url))
			{
				File.Delete(Main.ModsSavedBackupPath + mod.url);
			}

			if (File.Exists(Main.ModsSavedDisabledPath + mod.file))
			{
				File.Delete(Main.ModsSavedDisabledPath + mod.file);
			}

			if (File.Exists(Main.LuaPath + mod.file))
			{
				File.Delete(Main.LuaPath + mod.file);
			}

			if (Directory.Exists(Main.LuaPath + mod.folder))
			{
				Directory.Delete(Main.LuaPath + mod.folder, true);
			}

			var tempList = installedMods.ToList();
			foreach (var entry in tempList)
			{
				if (entry.title == mod.title)
				{
					tempList.Remove(entry);

					break;
				}
			}

			installedMods = tempList.ToArray();
			SaveInstalledMods();
			CheckInstalledMods();
		}

		public static void SaveInstalledMods()
		{
			File.WriteAllText(@Main.ModsInstalledFile, JsonHelper.ToJson(installedMods));
		}

		private void SwitchListClicked(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				//Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif
			SwitchListClicked();
		}

		private void SwitchListClicked()
		{
			switch (listMode)
			{
				case Mods.Installed:
					listMode           = Mods.Available;
					_listSwitcher.text = "Installed Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderInstalled.text = "Creator";

					break;
				case Mods.Available:
					listMode           = Mods.Installed;
					_listSwitcher.text = "Available Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderInstalled.text = "Installed";

					break;
				default:
					// WTF, Should never be set to Disabled!!
					throw new ArgumentException("SotAA.UI.Mods.Manager bad SwitchListClicked mode provided");

					break;
			}

			PopulateModsList();
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

		public IEnumerator UpdateMod(int id, bool doOnce, Action<bool> completed)
		{
			if (_alreadyDownloading)
			{
				yield break;
			}
			else
			{
				_alreadyDownloading = true;
			}

			//get the mod info from the web server
			Mod[] tempMod = new Mod[1];

			// ReSharper disable once StringLiteralTypo
			var www = UnityWebRequest.Get(WebSiteUrl + "getmods.php?update=1&request=" + id);

			//Use this if you have any problem with a certificate, need to set the public key into AcceptAllCertificatesSignedWithASpecificPublicKey.cs script
			www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
			var connection = www.SendWebRequest();

			while (!connection.isDone)
			{
				yield return null;
			}

			if (www.result == UnityWebRequest.Result.ConnectionError)
			{
				Debug.Log("Network Error: " + www.error + " " + www.responseCode);
			}
			else if (www.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.Log("HTTP Error: " + www.error + " " + www.responseCode);
			}
			else
			{
				string jsonString = FixJson(www.downloadHandler.text);
				Debug.Log(jsonString);
				if (jsonString != null)
				{
					tempMod = JsonHelper.FromJson<Mod>(@jsonString);
				}
			}

			Debug.Log(tempMod[0].url);

			// Update the mod with the new info
			www = UnityWebRequest.Get(WebSiteUrl + "mods/" + tempMod[0].url);

			// Use this if you have any problem with a certificat, need to set the public key into AcceptAllCertificatesSignedWithASpecificiedPublicKey.cs script
			www.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
			connection             = www.SendWebRequest();

			while (!connection.isDone)
			{
				yield return null;
			}

			if (www.result == UnityWebRequest.Result.ConnectionError)
			{
				Debug.Log("Network Error: " + www.error + " " + www.responseCode);
			}
			else if (www.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.Log("HTTP Error: " + www.error + " " + www.responseCode);
			}
			else
			{
				byte[] results     = www.downloadHandler.data;
				string zipSavePath = Main.ModsSavedBackupPath;
				string extractPath = Main.LuaPath;

				File.WriteAllBytes(zipSavePath + tempMod[0].url, results);
				using (ZipArchive archive = ZipFile.Open(zipSavePath + tempMod[0].url, ZipArchiveMode.Update))
				{
					ZipArchiveExtensions.ExtractToDirectory(archive, extractPath, true);
				}

				for (int i = 0; i < installedMods.Length; i++)
				{
					if (installedMods[i].title  == tempMod[0].title  ||
						installedMods[i].folder == tempMod[0].folder ||
						installedMods[i].file   == tempMod[0].file)
					{
						installedMods[i].creator   = tempMod[0].creator;
						installedMods[i].title     = tempMod[0].title;
						installedMods[i].desc      = tempMod[0].desc;
						installedMods[i].version   = tempMod[0].version;
						installedMods[i].deps      = tempMod[0].deps;
						installedMods[i].isdep     = tempMod[0].isdep;
						installedMods[i].icon      = tempMod[0].icon;
						installedMods[i].log       = tempMod[0].log;
						installedMods[i].folder    = tempMod[0].folder;
						installedMods[i].file      = tempMod[0].file;
						installedMods[i].backupzip = tempMod[0].url;

						//_installedMods[x].enabled = tempMod[0].enabled; //default is the mod currently installed
						if (!installedMods[i].enabled)
						{
							// Copy lua to disabled
							File.Move(Main.LuaPath               + installedMods[i].file,
									  Main.ModsSavedDisabledPath + installedMods[i].file
									 );
						}

						break;
					}
				}

				File.WriteAllText(Main.ModsInstalledFile, JsonHelper.ToJson(installedMods));

				// fully complete, write it down the cfg file
			}

			completed.Invoke(true);
			_alreadyDownloading = false;
		}
	}
}
