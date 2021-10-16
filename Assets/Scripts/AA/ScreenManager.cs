using System;
using AA.UI.Mods;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AA
{
	public class ScreenManager : MonoBehaviour
	{
		internal static VisualElement RootVe;
		private         MonoBehaviour _activeScript;
		private         DropdownField _avatars;
		private         int           _avatarIndex;
		private         VisualElement _mainScreen;
		private         VisualElement _modsScreen;
		private         string        _sceneName;
		private         VisualElement _startDialogue;
		private         VisualElement _startWindow;

		public Main Main;

		public ScreenManager()
		{
		}

		private void Start()
		{
			Main = new GameObject().AddComponent<Main>();
			if (Main == null)
			{
				throw new InvalidProgramException("ScreenManager: Failed to instantiate Main settings");
			}
			else if (Main.users.Count < 1)
			{
				throw new InvalidProgramException("ScreenManager: Instantiating Main failed to find users!!");
			}

			RootVe = GetComponent<UIDocument>().rootVisualElement;
			if (RootVe != null)
			{
				//Debug.Log("ScreenManager - RootVE: " + RootVe);
				_startDialogue = RootVe.Q<VisualElement>("StartDialogue");
				_startWindow   = RootVe.Q<VisualElement>("StartWindow");
				_mainScreen    = RootVe.Q<VisualElement>("MainScreen");
				_modsScreen    = RootVe.Q<VisualElement>("ModManager");

				//ShowStartDialogue();

				EnableModsScreen();
			}
			else
			{
				throw new Exception("ScreenManager Start: Unable to obtain Root Visual Element, cannot continue!");
			}
		}

		private void EnableModsScreen()
		{
			_sceneName = "ModManager";
			if (_mainScreen != null)
			{
				_mainScreen.style.display = DisplayStyle.None;
			}

			if (_modsScreen != null)
			{
				_modsScreen.style.display = DisplayStyle.Flex;

				//_activeScript             = new GameObject().AddComponent<Manager>();
				StartModManager();
			}
			else
			{
				throw new
					InvalidProgramException("ScreenManager - EnableModsScreen: mod screen visual element not found!!");
			}
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

		private void ShowStartDialogue()
		{
			_avatars = RootVe.Q<DropdownField>("AvatarDropdown");
			if (_avatars == null)
			{
				throw new
					InvalidProgramException("ScreenManager - ShowStartDialogue: Failed to get Avatar dropdown list!");
			}

			_avatarIndex     = 0;
			_avatars.choices = Main.users;
			_avatars.value   = Main.users[_avatarIndex];
			_avatars.RegisterCallback<ChangeEvent<string>>(
														   (evt) => { _avatars.value = evt.newValue; });
			_avatars.SetEnabled(true);


			_startWindow.style.display = DisplayStyle.Flex;
			_startWindow.visible       = true;
			_startDialogue.SetEnabled(true);
		}

		void StartModManager()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
#endif
				SceneManager.LoadSceneAsync(_sceneName);
#if UNITY_EDITOR
			else
				Debug.Log("Loading: " + _sceneName);
#endif
		}
	}
}
