using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AA
{
	public class ScreenManager : MonoBehaviour
	{
		private VisualElement _rootVE;
		private VisualElement _mainScreen;
		private VisualElement _modsScreen;

		public ScreenManager()
		{
		}

		private void Start()
		{
			_rootVE = GetComponent<UIDocument>().rootVisualElement;
			_mainScreen = _rootVE.Q("MainScreen");
			_modsScreen = _rootVE.Q("ModsManager");
			//_rootVE.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
			EnableModsScreen();
		}

		private void OnGeometryChange(GeometryChangedEvent evt)
		{
		}

		private void EnableModsScreen()
		{
			_mainScreen.style.display = DisplayStyle.None;
			_modsScreen.style.display = DisplayStyle.Flex;
		}

		private void SetPanelState(VisualElement panel, bool state)
		{
			if (state)
			{
				panel.style.display = DisplayStyle.Flex;
			}
			else
			{
				panel.style.display = DisplayStyle.None;
			}
		}
	}
}
