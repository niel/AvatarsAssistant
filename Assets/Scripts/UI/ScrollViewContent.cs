using UnityEngine.UIElements;

namespace UI
{
	public class ScrollViewContent : ScrollView
	{
		public ScrollViewContent()
		{
			name = "ScrollView-Container";
			AddToClassList("scrollview");
		}
	}
}
