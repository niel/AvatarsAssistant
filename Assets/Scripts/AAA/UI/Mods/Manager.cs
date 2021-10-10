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
using AAA.Utility;
using AAA.Web;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

namespace AAA.UI.Mods
{
	public class Manager : MonoBehaviour
	{
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

		private VisualElement _listContainer;
		private Mods          _listMode;

		public Mod[] deps;
		public Mod[] mods;

		public List<Mod>
			modsList; // List bound to ListView for displaying the others (installedModsList, mods, deps, etc.).

		private ListView _listViewContent;

		public VisualTreeAsset modItemTemplate;

		public Coroutine ActiveRoutine;

		[HideInInspector] public bool downloading;

		[HideInInspector] public DownloadStack downloadStack;

		// ReSharper disable once InconsistentNaming
		private VisualElement _rootVE;

		public static InstalledMod[] InstalledMods;

		private string _versions;
		private bool   _fetchingVersions;


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
			string configText = File.ReadAllText(@Main.ModsInstalled);

			// Destroy any previously created objects, before populating it again.
			modsList.Clear();

			// If there any mods found, we populate the list with them.
			if (configText.Length > 0)
			{
				InstalledMods = JsonHelper.FromJson<InstalledMod>(@configText);

				modsList.AddRange(InstalledMods);
				modsList.Sort();

				_versions = "";
				for (int i = 0; i < InstalledMods.Length; i++)
				{
					_versions = _versions + ";" + InstalledMods[i].id;
				}

				_versions = _versions[1..];

				ActiveRoutine = StartCoroutine(CheckVersions(InstalledMods.Length));
			}
			else // If the file is less than 1, there are no mods found!
			{
				//throw new NotImplementedException("CheckInstalledMods: Empty Config File");
				Debug.Log("CheckInstalledMods: No Configuration in file.");

				InstalledMods = JsonHelper.FromJson<InstalledMod>(@NothingFoundJson);
				modsList.AddRange(InstalledMods);
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

			for (int i = 0; i < InstalledMods.Length; i++)
			{
				InstalledMods[i].latest = versions[i];
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
					Debug.Log("Mods: " + _jsonString);

					mods = JsonHelper.FromJson<Mod>(_jsonString);
					Debug.Log("Mods[]: " + mods.Length);
					PopulateModsList(false);

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

		public static IEnumerator GetModZip(DownloadStack dlstack, InstalledMod[] installedModsList,
											Action<bool>  completed)
		{
			throw new NotImplementedException("GetModZip");
		}

		/// <summary>
		/// OnEnable is called when the object becomes enabled and active.
		/// </summary>
		private void OnEnable()
		{
			_rootVE = GetComponent<UIDocument>().rootVisualElement;

			#region Initialise elements for easy access.

			_columnHeaderInstalled = _rootVE.Q<Label>("ColumnHeader-Installed");
			_columnHeaderLatest    = _rootVE.Q<Label>("ColumnHeader-Latest");
			_listSwitcher          = _rootVE.Q<Button>("Button-ModsListSwitch");
			_startLauncher         = _rootVE.Q<Button>("Button-StartLauncher");
			_startSotA             = _rootVE.Q<Button>("Button-StartSotA");
			_listContainer         = _rootVE.Q<VisualElement>("ListContainer");

			_startLauncher.visible = false;
			_startSotA.visible     = false;

			_listSwitcher.RegisterCallback<ClickEvent>(ev => SwitchListClicked());

			// TODO add detection code for location of Launcher/SotA Binary. Enable buttons and callbacks if found.
			//m_StartLauncher.RegisterCallback<ClickEvent>(ev => StartLauncherClicked());
			//m_StartSotA.RegisterCallback<ClickEvent>(ev => StartSotAClicked());

			#endregion

			#region Prepare the ListVew element.

			// The "makeItem" function is called when the ListView needs more items to render.
			Func<VisualElement> makeItem = () => new ModItem();

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
			_listMode = Mods.Available;
			SwitchListClicked();
		}

		private void PopulateModsList(bool getDependencies = false)
		{
			// First we have to get our list of Mods. How we do that depends on the list we are interested in.
			switch (_listMode)
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

			var tempList = InstalledMods.ToList();
			foreach (var entry in tempList)
			{
				if (entry.title == mod.title)
				{
					tempList.Remove(entry);
					break;
				}
			}

			InstalledMods = tempList.ToArray();
			CheckInstalledMods();
		}

		public static void SaveInstalledMods()
		{
			File.WriteAllText(@Main.ModsInstalled, JsonHelper.ToJson(InstalledMods));
		}

		private void SwitchListClicked()
		{
			switch (_listMode)
			{
				case Mods.Installed:
					_listMode          = Mods.Available;
					_listSwitcher.text = "Installed Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderLatest.text = "Creator";

					break;
				case Mods.Available:
					_listMode          = Mods.Installed;
					_listSwitcher.text = "Available Mods";

					//_columnHeaderInstalled.text = "Current";
					_columnHeaderLatest.text = "Newest";

					break;
				default:
					// WTF, Should never be set to Disabled!!
					throw new ArgumentException("SotAA.UI.Mods.Manager bad SwitchListClicked mode provided");

					break;
			}

			PopulateModsList(false);
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

				for (int i = 0; i < InstalledMods.Length; i++)
				{
					if (InstalledMods[i].title == tempMod[0].title ||
						InstalledMods[i].folder == tempMod[0].folder ||
						InstalledMods[i].file  == tempMod[0].file)
					{
						InstalledMods[i].creator   = tempMod[0].creator;
						InstalledMods[i].title     = tempMod[0].title;
						InstalledMods[i].desc      = tempMod[0].desc;
						InstalledMods[i].version   = tempMod[0].version;
						InstalledMods[i].deps      = tempMod[0].deps;
						InstalledMods[i].isdep     = tempMod[0].isdep;
						InstalledMods[i].icon      = tempMod[0].icon;
						InstalledMods[i].log       = tempMod[0].log;
						InstalledMods[i].folder    = tempMod[0].folder;
						InstalledMods[i].file      = tempMod[0].file;
						InstalledMods[i].backupzip = tempMod[0].url;

						//_installedMods[x].enabled = tempMod[0].enabled; //default is the mod currently installed
						if (!InstalledMods[i].enabled)
						{
							// Copy lua to disabled
							File.Move(Main.LuaPath + InstalledMods[i].file,
									  Main.ModsSavedDisabledPath + InstalledMods[i].file
									  );
						}

						break;
					}
				}

				File.WriteAllText(Main.ModsInstalled, JsonHelper.ToJson(InstalledMods));

				// fully complete, write it down the cfg file
			}

			completed.Invoke(true);
			_alreadyDownloading = false;
		}
	}
}
