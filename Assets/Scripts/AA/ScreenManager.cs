using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AA
{
	public class ScreenManager : MonoBehaviour
	{
		internal static VisualElement rootVe;
		private         Coroutine     _activeRoutine;
		private         MonoBehaviour _activeScript;
		private         DropdownField _avatars;
		private         int           _avatarIndex;
		private         VisualElement _mainScreen;
		private         VisualElement _modsScreen;
		private         string        _sceneName;
		private         VisualElement _startDialogue;
		private         VisualElement _startWindow;

		public Main path;

		private void Awake()
		{
			rootVe = GetComponent<UIDocument>().rootVisualElement;

			if (rootVe != null)
			{
				//Debug.Log("ScreenManager - RootVE: " + RootVe);
				_mainScreen = rootVe.Q<VisualElement>("MainScreen");
				_modsScreen = rootVe.Q<VisualElement>("ModManager");
			}
			else
			{
				throw new Exception("ScreenManager Start: Unable to obtain Root Visual Element, cannot continue!");
			}
		}

		private void InitialisationCompleted()
		{
			if (path == null)
			{
				throw new InvalidProgramException("ScreenManager: Failed to instantiate Main settings");
			}
			else if (path.users.Count < 1)
			{
				throw new InvalidProgramException("ScreenManager: Instantiating Main failed to find users!!");
			}

			Debug.Log("ScreenManager: Start - paths initialised!");

			//throw new NotImplementedException("Stopping after initialisaton");
			EnableModsScreen();
		}

		private void Start()
		{
			path           = new GameObject().AddComponent<Main>();
			_activeRoutine = StartCoroutine(Main.StartDialogueDone(InitialisationCompleted));
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
			panel.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
		}

		void StartModManager()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
#endif
			{
				SceneManager.LoadSceneAsync(_sceneName);
			}
#if UNITY_EDITOR
			else
			{
				Debug.Log("Loading: " + _sceneName);
			}
#endif
		}
	}
}
