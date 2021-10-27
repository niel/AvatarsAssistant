using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AA.Logs;
using AA.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AA
{
	public class Main : MonoBehaviour
	{
#if DEBUG
		private const bool Testing = true;
#else
		private const bool   Testing = false;
#endif

		#region Constants and Variables

		private const string ChatLogsPattern = @"SotaChatLog_(?<name>[\w ]+)_(?<date>\d{4}-\d{2}-\d{2})";

		private static Coroutine _activeRoutine;

		//public         AppLocation[] apps     = new AppLocation[2];
		public static   List<string>  appNames = new List<string>(2);
		public static   List<string>  appPaths = new List<string>(2);
		private         DropdownField _avatars;
		private static  string        _chatLogsPath;
		internal static DropdownField connectTo;
		public          bool          IsInitialized { get; private set; }
		private static  string        _luaPath;
		private static  string        _modsInstalledFile;
		private static  string        _modsSavedPath;
		private static  string        _modsSavedBackupPath;
		private static  string        _modsSavedDisabledPath;
		private static  VisualElement _rootVe;
		private static  string        _sotaAppPath;
		private         VisualElement _startDialogue;
		private static  bool          _startDialogueContinue;
		private static  object        _startWindow;
		public          List<string>  users = new List<string>();

		public static string ChatLogsPath
		{
			get { return _chatLogsPath; }
		}

		public static string LuaPath
		{
			get { return _luaPath; }
		}

		public static string ModsInstalledFile
		{
			get { return _modsInstalledFile; }
		}

		public static string ModsSavedBackupPath
		{
			get { return _modsSavedBackupPath; }
		}

		public static string ModsSavedDisabledPath
		{
			get { return _modsSavedDisabledPath; }
		}

		public static string ModsSavedPath
		{
			get { return _modsSavedPath; }
		}

		public static string SotaAppPath
		{
			get { return _sotaAppPath; }
			set { _sotaAppPath = value; }
		}

		#endregion


		private void Awake()
		{
			#region Get Application Paths

			string baseAppInstallLocation;
			var    qaSuffix   = @"/Portalarium/Shroud of the Avatar(QA)/";
			var    mainSuffix = @"/Portalarium/Shroud of the Avatar/";

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

			if (Directory.Exists(@baseAppInstallLocation + @mainSuffix))
			{
				appPaths.Insert(0, @baseAppInstallLocation + @mainSuffix);
				appNames.Insert(0, "Live");
			}

			if (Directory.Exists(@baseAppInstallLocation + @qaSuffix))
			{
				appPaths.Add(@baseAppInstallLocation + @qaSuffix);
				appNames.Add("QA");
			}

			#endregion

			#region Initialise VisualElements

			var rootVe = ScreenManager.rootVe;

			//var rootVe = GetComponent<UIDocument>().rootVisualElement;

			_avatars       = rootVe.Q<DropdownField>("AvatarDropdown");
			_startDialogue = rootVe.Q<VisualElement>("StartDialogue");
			_startWindow   = rootVe.Q<VisualElement>("StartWindow");

/*
			_startWindow.SetEnabled(true);
			_startWindow.visible       = true;
			_startWindow.style.display = DisplayStyle.Flex;
*/

			_startDialogue.SetEnabled(true);
			_startDialogue.visible       = true;
			_startDialogue.style.display = DisplayStyle.Flex;

			_avatars.SetEnabled(false);
			_avatars.visible       = false;
			_avatars.style.display = DisplayStyle.None;

			#endregion

			#region Set up the StartDialogue

			var names = new List<string>();
			foreach (var appName in appNames)
			{
				names.Add(appName);
				Debug.Log("Name: " + appName);
			}

			connectTo         = rootVe.Q<DropdownField>("WorldDropdown");
			connectTo.visible = true;
			connectTo.choices = names;
			connectTo.RegisterCallback<ChangeEvent<string>>(evt => { connectTo.value = evt.newValue; });

			switch (names.Count)
			{
				case 0: // Error. Should be able to find at least one valid path!
					throw new Exception("Cannot find SotA's application path");

					break;
				case 1: // Single entry, so there is no choice to be made.
					connectTo.value = appNames[0];

					break;
				case 2 when Testing is true: // A choice, but we're in DEBUG mode so stick to QA.
					Debug.Log("Two or more Application paths available, using the QA one as TEST mode IS enabled.");
					connectTo.value = appNames[1];

					// TODO change this to find index of "QA" in names and use that to assign text.

					break;
				default: // Choice for the user to make.
					// TODO add selection code/dialogue box
					Debug.Log("Two or more Application paths available, using the first one as TEST mode is not enabled.");
					connectTo.value = appNames[0];

					break;
			}

			var ok = _startDialogue.Q<Button>("Continue");
			ok.SetEnabled(true);
			ok.AddToClassList("button-enabled");
			ok.RegisterCallback<ClickEvent>(ClickedContinue, TrickleDown.NoTrickleDown);

			#endregion
		}

		internal void ClickedContinue(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif

			SotaAppPath = appPaths[connectTo.index];
			Debug.Log("App path set to: " + SotaAppPath);

			_startDialogue.SetEnabled(false);
			_startDialogue.visible       = false;
			_startDialogue.style.display = DisplayStyle.None;

			IsInitialized = true;

			InitialisePaths();
			InitialiseUsers();
		}

		private void InitialisePaths()
		{
			Debug.Log("Main.SetPaths: Entered!");

			#region Configure files and paths.

			_chatLogsPath = SotaAppPath + @"ChatLogs/";
			_luaPath      = SotaAppPath + @"Lua/";

			#region Directory Checks

			_modsSavedPath = SotaAppPath + @"SavedMods/";

			// Check that necessary directories and files exist.
			if (!Directory.Exists(ModsSavedPath))
			{
				Directory.CreateDirectory(ModsSavedPath);
			}

			_modsSavedBackupPath = SotaAppPath + @"SavedMods/backup/";
			if (!Directory.Exists(ModsSavedBackupPath))
			{
				Directory.CreateDirectory(ModsSavedBackupPath);
			}

			_modsSavedDisabledPath = SotaAppPath + @"SavedMods/disabled/";
			if (!Directory.Exists(ModsSavedDisabledPath))
			{
				Directory.CreateDirectory(ModsSavedDisabledPath);
			}

			#endregion

			// Check if our settings file exists. if not, create it (this only saves the location of the launcher)
			if (!File.Exists(SotaAppPath + @"SavedMods/Settings.cfg"))
			{
				//File.CreateText(AppPath + @"/SavedMods/Settings.cfg");
				//create only when the button is clicked
			}
			else
			{
				JsonUtility.FromJson<LauncherLocation>(File.ReadAllText(SotaAppPath + @"SavedMods/Settings.cfg"));
			}

			// Check if the installed mods config file is there, if not we create it.
			_modsInstalledFile = ModsSavedPath + @"InstalledMods.cfg";
			if (!File.Exists(ModsInstalledFile))
			{
				File.CreateText(ModsInstalledFile);
			}

			#endregion
		}

		private void InitialiseUsers()
		{
			string[] files = Directory.GetFiles(ChatLogsPath);

			users.Clear();
			foreach (var file in files)
			{
				var fileProps = new FileInfo(file);
				var match     = Regex.Match(fileProps.Name, ChatLogsPattern, RegexOptions.IgnoreCase);
				var user      = match.Groups["name"].ToString();
				if (match.Success && !users.Contains(user))
				{
					//Debug.Log("Adding name: " + user);
					users.Add(user);
				}
			}

			users.Sort();

			_startDialogueContinue = true;
		}

		private void ProcessLogs(string avatar)
		{
			var pattern          = "SotAChatLog_" + avatar + "*.txt";
			var filteredChatLogs = Directory.EnumerateFiles(ChatLogsPath, @pattern);

			//var parser = new Parser(avatar, ChatLogsPath + "SotAChatLog_Archer_2021-10-05.txt");
/*

			foreach (var file in filteredChatLogs)
			{
				//Debug.Log("File: " + file);
				var fileProps = new FileInfo(file);
				//var match     = Regex.Match(fileProps.Name, ChatLogsPattern);
				var parser = new Parser(file);
			}
*/
		}

		public static IEnumerator StartDialogueDone(Action completed)
		{
			yield return new WaitUntil(() => _startDialogueContinue);
			completed();
		}
	}
}
