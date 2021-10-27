using UnityEngine;
using UnityEngine.UIElements;

namespace AA.UI
{
	public class Utilities
	{
		private static long _lastClick;

		public void ShowChildren(VisualElement element)
		{
			if (element.childCount > 0)
			{
				foreach (var child in element.Children())
				{
					Debug.Log("Child: " + child.name);
					ShowChildren(child);
				}
			}
		}

#if UNITY_STANDALONE_LINUX
		public static bool VoidDuplicateClick(ClickEvent evt)
		{
			if (Mathf.Abs(evt.timestamp - _lastClick) < 100)
			{
				//Debug.Log("Ignoring duplicate click!");
				return true;
			}

			_lastClick = evt.timestamp;

			return false;
		}
#endif
	}
}
