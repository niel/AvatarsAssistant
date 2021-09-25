using System.Runtime.InteropServices;
using UnityEngine.UIElements;

namespace UI
{
	public class ModEntry : VisualElement
	{
		public Mod ModObject;

		private Label     _name;
		private Label     _desc;
		private Label     _installed;
		private Label     _latest;
		private ButtonBar _buttonBar;

		private class ButtonBar
		{
			public Button BtnEnabled;
			public Button BtnUpdate;
			public Button BtnRemove;

			public ButtonBar()
			{
				;
			}
		}

		private void BtnEnabledClicked(Button btn)
		{
			// TODO determine if we're enabling or disabling and act accordingly;
			if (btn.text == "Enable")
			{
				// We should enable.
				btn.text = "Disable";
			}
			else
			{
				// We should disable.
				btn.text = "Enable";
			}
		}

		private void BtnRemoveClicked(Button btn)
		{
			// TODO determine if we're enabling or disabling and act accordingly;
			if (btn.text == "")
			{
				// We should enable.
				btn.text = "";
			}
			else
			{
				// We should disable.
				btn.text = "";
			}
		}

		private void BtnUpdateClicked(Button btn)
		{
			// TODO determine if we're enabling or disabling and act accordingly;
			if (btn.text == "")
			{
				// We should enable.
				btn.text = "";
			}
			else
			{
				// We should disable.
				btn.text = "";
			}
		}

		public ModEntry(Mod mod)
		{
			ModObject  = mod;
			_buttonBar = new ButtonBar();

			_name                 = new Label(ModObject.title);
			_desc                 = new Label(ModObject.desc);
			_installed            = new Label(ModObject.version);
			_buttonBar.BtnEnabled = new Button();
			_buttonBar.BtnRemove  = new Button();
			_buttonBar.BtnUpdate  = new Button();

			_buttonBar.BtnEnabled.RegisterCallback<ClickEvent>(ev => BtnEnabledClicked(_buttonBar.BtnEnabled));
			_buttonBar.BtnRemove.RegisterCallback<ClickEvent>(ev => BtnRemoveClicked(_buttonBar.BtnEnabled));
			_buttonBar.BtnUpdate.RegisterCallback<ClickEvent>(ev => BtnUpdateClicked(_buttonBar.BtnEnabled));
			_latest      = new Label
						   {
							   text = ""
						   };
		}
	}
}
