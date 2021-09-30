using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
	public class ModItem : VisualElement
	{
		public ModItem()
		{
			AddToClassList("list-item");

		}

		public void BindEntry(Mod entry)
		{
			Debug.Log("ModItem: BindItem entered!");
			if (entry.title != "NoModsFound")
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/ModItem");
				Add(modItemTemplate.Instantiate());

				var title     = this.Q<Label>("Name");
				var desc      = this.Q<Label>("Description");
				var installed = this.Q<Label>("Installed");
				var latest    = this.Q<Label>("Latest");
				var enable    = this.Q<Button>("Enabled");
				var update    = this.Q<Button>("Update");
				var remove    = this.Q<Button>("Remove");

				title.text     = entry.title;
				desc.text      = entry.desc;
				installed.text = entry.version;
				latest.text    = "";

				if (entry.enabled)
				{
					enable.text    = "Disable";
					enable.visible = true;
				}
				//enable.RegisterCallback<ClickEvent>(ev => ClickedEnabled());
				//update.RegisterCallback();
				//remove.RegisterCallback();
			}
			else
			{
				var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/NoModsFound");
				Add(modItemTemplate.Instantiate());
			}
		}

		private void ClickedEnabled()
		{
			// is variable 'ev' passed in by the callback?
			// TODO handle enabling or disabling as needed
			//Debug.Log("ev is: " + evt);
		}
	}
}
