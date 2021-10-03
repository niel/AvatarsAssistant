using UnityEngine;
using UnityEngine.UIElements;

namespace SotaAssistant.UI
{
	public class Utilities
	{
		private void ShowChildren(VisualElement element)
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
	}
}
