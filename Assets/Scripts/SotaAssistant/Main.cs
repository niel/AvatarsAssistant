using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SotaAssistant
{
	public class Main : MonoBehaviour
	{
		private const  bool   _testing = true;

		private static   string       _chatLogsPath;
		private static   string       _luaPath;
		private static   string       _modsInstalled;
		private static   string       _modsSavedPath;
		private static   string       _modsSavedBackupPath;
		private static   string       _modsSavedDisabledPath;
		private readonly string       _sotaAppPath;
		public           List<string> users;

		public string SotaAppPath { get { return _sotaAppPath; }}

		public static string ModsInstalled { get { return _modsInstalled; }}

		public static string ChatLogsPath { get { return _chatLogsPath; }}

		public static string LuaPath { get { return _luaPath; }}

		public static string ModsSavedPath { get { return _modsSavedPath; }}

		public static string ModsSavedBackupPath { get { return _modsSavedBackupPath; }}

		public static string ModsSavedDisabledPath { get { return _modsSavedDisabledPath; }}


		public Main()
		{
			#region Configure files and paths.
			string sotaDirectory = _testing
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
			_sotaAppPath = @baseAppInstallLocation + @sotaDirectory;
			_chatLogsPath = SotaAppPath             + @"ChatLogs/";
			_luaPath      = SotaAppPath             + @"Lua/";

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

			/*
			// Check if our settings file exists. if not, create it (this only saves the location of the launcher)
			if (!File.Exists(_dataPath + @"SavedMods/Settings.cfg"))
			{
				//File.CreateText(AppPath + @"/SavedMods/Settings.cfg");
				//create only when the button is clicked
			}
			else
			{
				JsonUtility.FromJson<LauncherLocation>(File.ReadAllText(_dataPath + @"SavedMods/Settings.cfg"));
			}
			*/

			// Check if the installed mods config file is there, if not we create it.
			_modsInstalled = ModsSavedPath + @"InstalledMods.cfg";
			if (!File.Exists(ModsInstalled))
			{
				File.CreateText(ModsInstalled);
			}
			#endregion

			users = new List<string>();
			Users();
		}

		public void Users()
		{
			string[] files     = Directory.GetFiles(Main.ChatLogsPath);
			var pattern = @"SotaChatLog_(?<name>[\w ]+)_(?<date>\d{4}-\d{2}-\d{2})";
			users.Clear();
			foreach (string file in files)
			{
				var fileProps = new FileInfo(file);
				var match     = Regex.Match(fileProps.Name, @pattern, RegexOptions.IgnoreCase);
				var user      = match.Groups["name"].ToString();
				if (match.Success && !users.Contains(user))
				{
					Debug.Log("Adding name: " + user);
					users.Add(user);
				}
			}
			users.Sort();
		}
	}
}
