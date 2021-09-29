using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
	public class ModItem : VisualElement
	{
		public ModItem()
		{
			AddToClassList("list-item");

			var modItemTemplate = Resources.Load<VisualTreeAsset>("UI/ModItem");
			Add(modItemTemplate.Instantiate());
		}

		public void BindEntry(Mod entry)
		{
			Debug.Log("ModItem: BindItem entered!");
			if (entry.title != "NoModsFound")
			{
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
					enable.text = "Disable";
				}
				//enable.RegisterCallback<ClickEvent>(ev => ClickedEnabled());
				//update.RegisterCallback();
				//remove.RegisterCallback();
			}
			else
			{
				var container = this.Q<VisualElement>("TextContent");

				foreach (var child in container.Children())
				{
					container.Remove(child);
				}

				var emptyTemplate = new Label
									{
										name = "EmptyItem",
										text = "Currently there are no mods installed.&#10;Click the &apos;Available mods&apos; button to see a list of those available."
									};
				emptyTemplate.AddToClassList("empty-list");
				container.Add(emptyTemplate);
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
