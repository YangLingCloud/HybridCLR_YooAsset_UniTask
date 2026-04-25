using UnityEngine;

namespace UniFramework.Event
{
	/// <summary>
	/// UniEvent 的 Unity 生命周期驱动器，负责每帧刷新延迟事件队列。
	/// </summary>
	internal class UniEventDriver : MonoBehaviour
	{
		/// <summary>
		/// Unity 每帧回调，转发给 UniEvent.Update。
		/// </summary>
		void Update()
		{
			UniEvent.Update();
		}
	}
}
