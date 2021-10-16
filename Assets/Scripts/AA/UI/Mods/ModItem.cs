using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace AA.UI.Mods
{
	public class ModItem : VisualElement
	{
		private          Coroutine _activeRoutine;
		private          Button    _button1;
		private          Button    _button2;
		private          Button    _button3;
		private          bool      _completed = false;
		private          string    _lastClick;
		private readonly Manager   _manager;
		private          Mod       _source;

		public Label desc;
		public Label installed;
		public Label latest;
		public Label title;

		public ModItem(Manager manager)
		{
			AddToClassList("list-item");
			_manager = manager;
		}

		public void BindEntry(Mod entry)
		{
			_source = entry;

			if (entry.title == "NoModsFound")
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/Mods/NoModsFound");
				Add(modItemTemplate.Instantiate());

				return;
			}
			else
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/Mods/ModItem");
				Add(modItemTemplate.Instantiate());

				title     = this.Q<Label>("Name");
				desc      = this.Q<Label>("Description");
				installed = this.Q<Label>("Installed");
				latest    = this.Q<Label>("Latest");
				_button1  = this.Q<Button>("ButtonEnableDisable");
				_button2  = this.Q<Button>("ButtonUpdate");
				_button3  = this.Q<Button>("ButtonRemove");

				title.text     = _source.title;
				desc.text      = _source.desc;
				installed.text = _source.version;
				latest.text    = _source.latest;
			}

			if (_manager.listMode == Mods.Available)
			{
				BindAvailable();
			}
			else
			{
				BindInstalled();
			}
		}

		private void BindAvailable()
		{
			latest.text = _source.creator;
			_button1.SetEnabled(false);
			_button1.style.display = DisplayStyle.None;
			_button3.SetEnabled(false);
			_button3.style.display = DisplayStyle.None;

			_button2.text = "Install";
			_button2.RegisterCallback<ClickEvent>(ClickedInstall, TrickleDown.NoTrickleDown);
			_button2.AddToClassList("button-enabled");
		}

		private void BindInstalled()
		{
			if (latest.text == "")
			{
				latest.text = "Fetching...";
			}
			else if (_source.version == _source.latest)
			{
				latest.text = "Yes";
				latest.AddToClassList("version-current");
				installed.AddToClassList("version-current");
				_button2.SetEnabled(true);
			}
			else
			{
				installed.AddToClassList("version-older");
			}

			_button1.text = _source.enabled ? "Disable" : "Enable";
			_button1.RegisterCallback<ClickEvent>(ClickedEnabled, TrickleDown.TrickleDown);
			_button1.AddToClassList("button-enabled");
			if (_source.version != _source.latest)
			{
				_button2.RegisterCallback<ClickEvent>(ClickedUpdate, TrickleDown.TrickleDown);
				_button2.AddToClassList("button-enabled");
			}
			else
			{
				_button2.SetEnabled(false);
			}

			_button3.RegisterCallback<ClickEvent>(ClickedRemove, TrickleDown.TrickleDown);
			_button3.AddToClassList("button-enabled");
		}

		private void ClickedEnabled(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif

			//var enable = this.Q<Button>("ButtonEnableDisable");
			var enable = (Button) evt.target;
/*
			if (_lastClick == enable.text)
			{
				return;
			}
*/

			// Disable the button while we are processing it.
			enable.SetEnabled(false);

			if (enable.text == "Enable")
			{
				var source = Main.ModsSavedDisabledPath + _source.file;
				var target = Main.LuaPath               + _source.file;

				//Debug.Log("Source: " + source);
				//Debug.Log("Target: " + target);
				try
				{
					Debug.Log("Trying to enable");
					if (File.Exists(source))
					{
						//Debug.Log("Moving the file!");
						File.Move(@source, @target);

						//Debug.Log("Moved?");
					}
					else
					{
						Debug.Log("File not found!");
					}
				}
				catch (Exception e)
				{
					Debug.Log(e);

					throw;
				}

				for (int i = 0; i < Manager.InstalledMods.Length; i++)
				{
					if (Manager.InstalledMods[i].file == _source.file)
					{
						Manager.InstalledMods[i].enabled = true;
					}
				}

				enable.text = "Disable";
			}
			else if (enable.text == "Disable")
			{
				var source = @Main.LuaPath               + _source.file;
				var target = @Main.ModsSavedDisabledPath + _source.file;

				//Debug.Log("Source: "             + source);
				//Debug.Log("Target: "             + target);
				try
				{
					Debug.Log("Trying to disable");
					if (File.Exists(source))
					{
						//Debug.Log("Moving the file!");
						File.Move(@source, @target);

						//Debug.Log("Moved?");
					}
					else
					{
						Debug.Log("File not found!");
					}
				}
				catch (Exception e)
				{
					Debug.Log(e);

					throw;
				}

				for (int i = 0; i < Manager.InstalledMods.Length; i++)
				{
					if (Manager.InstalledMods[i].file == _source.file)
					{
						Manager.InstalledMods[i].enabled = false;
					}
				}

				enable.text = "Enable";
			}

			Manager.SaveInstalledMods();

			_lastClick = enable.text;
			enable.SetEnabled(true);
		}

		private void ClickedInstall(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif
			;
		}

		private void ClickedRemove(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif
			_manager.RemoveMod(_source);
		}

		private void ClickedUpdate(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			if (Utilities.VoidDuplicateClick(evt))
			{
				Debug.Log("Ignoring duplicate click event!");

				return;
			}
#endif
			Debug.Log("ModItem: ClickedUpdate");

			var update = (Button) evt.target;
			update.SetEnabled(false);
			_completed = false;

			_activeRoutine = _manager.StartCoroutine(_manager.UpdateMod(_source.id, true, value => _completed = value));
		}
	}
}
