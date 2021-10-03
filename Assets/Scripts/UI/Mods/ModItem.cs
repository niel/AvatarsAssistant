using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Mods
{
	public class ModItem : VisualElement
	{
		private Mod       _source;

		public ModItem()
		{
			AddToClassList("list-item");

		}

		public void BindEntry(Mod entry)
		{
			_source = entry;
			//mb      = gameObject.AddComponent<MonoBehaviour>();

			if (entry.title != "NoModsFound")
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/ModItem");
				Add(modItemTemplate.Instantiate());

				//this.name = "listEntry-" + entry.title;
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
					update.SetEnabled(false);
				}
				else
				{
					installed.AddToClassList("version-older");
				}

				enable.text = entry.enabled ? "Disable" : "Enable";
				if (enable.text == "enable")
				{
					enable.RegisterCallback<ClickEvent>(ev => ClickedEnabled(enable));
				}
				else
				{
					enable.RegisterCallback<ClickEvent>(ev => ClickedDisabled(enable));
				}
				enable.AddToClassList("button-enabled");
				//update.RegisterCallback(ClickedUpdate(update));
				//remove.RegisterCallback<ClickEvent>(ev => ClickedRemove(remove));
				//remove.AddToClassList("button-enabled");
			}
			else
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/NoModsFound");
				Add(modItemTemplate.Instantiate());
			}
		}

		private void ClickedEnabled(Button enable)
		{
			//var enable = this.Q<Button>("ButtonEnableDisable");
			//Disable the button while we are processing it.
			enable.SetEnabled(false);
			// TODO handle enabling or disabling as needed
			if (enable.text == "Enable")
			{
				var source = Manager.DataPath + @"SavedMods/disabled/" + _source.file;
				var target = Manager.DataPath + @"Lua/"                + _source.file;
				//Debug.Log("Source: " + source);
				//Debug.Log("Target: " + target);
				try
				{
					Debug.Log("Trying to enable");
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
				for (int i = 0; i < Manager.installedMods.Length; i++)
				{
					if (Manager.installedMods[i].file == _source.file)
					{
						Manager.installedMods[i].enabled = true;
					}
				}

				enable.text = "Disable";
				enable.UnregisterCallback<ClickEvent>(ev => ClickedEnabled(enable));
				enable.RegisterCallback<ClickEvent>(ev => ClickedDisabled(enable));
				Manager.SaveInstalledMods();
			}

			enable.SetEnabled(true);
		}

		private void ClickedDisabled(Button enable)
		{
			enable.SetEnabled(false);
			if (enable.text == "Disable")
			{
				var source = Manager.DataPath + @"Lua/"                + _source.file;
				var target = Manager.DataPath + @"SavedMods/disabled/" + _source.file;
				//Debug.Log("Source: "             + source);
				//Debug.Log("Target: "             + target);
				try
				{
					Debug.Log("Trying to disable");
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

				for (int i = 0; i < Manager.installedMods.Length; i++)
				{
					if (Manager.installedMods[i].file == _source.file)
					{
						Manager.installedMods[i].enabled = false;
					}
				}

				enable.text = "Enable";
				enable.UnregisterCallback<ClickEvent>(ev => ClickedDisabled(enable));
				enable.RegisterCallback<ClickEvent>(ev => ClickedEnabled(enable));
				Manager.SaveInstalledMods();
			}

			enable.SetEnabled(true);
		}

		private void ClickedRemove(Button remove)
		{
		}

		private void ClickedUpdate(Button update)
		{
		}
	}
}
