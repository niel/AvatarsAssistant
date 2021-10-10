using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace AA.UI.Mods
{
	public class ModItem : VisualElement
	{
		private          Coroutine  _activeRoutine;
		private          bool       _completed = false;
		private          GameObject _go;
		private readonly Manager    _manager;
		private          Mod        _source;

		public ModItem()
		{
			AddToClassList("list-item");
			var mb = new GameObject().AddComponent<MonoBehaviour>();
			_manager = mb.GetComponent<Manager>();
		}

		public void BindEntry(Mod entry)
		{
			_source = entry;

			if (entry.title != "NoModsFound")
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/Mods/ModItem");
				Add(modItemTemplate.Instantiate());

				var title     = this.Q<Label>("Name");
				var desc      = this.Q<Label>("Description");
				var installed = this.Q<Label>("Installed");
				var latest    = this.Q<Label>("Latest");
				var enable    = this.Q<Button>("ButtonEnableDisable");
				var update    = this.Q<Button>("ButtonUpdate");
				var remove    = this.Q<Button>("ButtonRemove");

				title.text     = entry.title;
				desc.text      = entry.desc;
				installed.text = entry.version;
				latest.text    = entry.latest;

				if (latest.text == "")
				{
					latest.text = "Fetching...";
				}
				else if (latest.text == installed.text)
				{
					latest.text = "Yes";
					latest.AddToClassList("version-current");
					installed.AddToClassList("version-current");
					update.SetEnabled(true);
				}
				else
				{
					installed.AddToClassList("version-older");
				}

				enable.text = entry.enabled ? "Disable" : "Enable";
				enable.RegisterCallback<ClickEvent>(ClickedEnabled, TrickleDown.TrickleDown);
				enable.AddToClassList("button-enabled");
				update.RegisterCallback<ClickEvent>(ClickedUpdate, TrickleDown.TrickleDown);
				update.AddToClassList("button-enabled");
				remove.RegisterCallback<ClickEvent>(ClickedRemove, TrickleDown.TrickleDown);
				remove.AddToClassList("button-enabled");
			}
			else
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/Mods/NoModsFound");
				Add(modItemTemplate.Instantiate());
			}
		}

		private void ClickedEnabled(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			Utilities.VoidDuplicateClick(evt);
#endif

			var enable = (Button)evt.target;
			//var enable = this.Q<Button>("ButtonEnableDisable");

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
					//Debug.Log("Trying to enable");
					if (File.Exists(source))
					{
						Debug.Log("Moving the file!");
						File.Move(@source, @target);
						Debug.Log("Moved?");
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
					//Debug.Log("Trying to disable");
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

			enable.SetEnabled(true);
		}

		private void ClickedRemove(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			Utilities.VoidDuplicateClick(evt);
#endif
			_manager.RemoveMod(_source);
		}

		private void ClickedUpdate(ClickEvent evt)
		{
#if UNITY_STANDALONE_LINUX
			Utilities.VoidDuplicateClick(evt);
#endif
			Debug.Log("ModItem: ClickedUpdate");

			var update = (Button)evt.target;
			update.SetEnabled(false);
			_completed     = false;

			_activeRoutine = _manager.StartCoroutine(_manager.UpdateMod(_source.id, true, value => _completed = value));
		}
	}
}
