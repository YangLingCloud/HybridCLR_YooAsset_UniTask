using System;
using System.Collections;
using System.Collections.Generic;

namespace UniFramework.Event
{
	/// <summary>
	/// 事件组封装，用于集中管理某个对象注册的事件监听并统一释放。
	/// </summary>
	public class EventGroup
	{
		private readonly Dictionary<System.Type, List<Action<IEventMessage>>> _cachedListener = new Dictionary<System.Type, List<Action<IEventMessage>>>();

		/// <summary>
		/// 添加一个监听
		/// </summary>
		public void AddListener<TEvent>(System.Action<IEventMessage> listener) where TEvent : IEventMessage
		{
			System.Type eventType = typeof(TEvent);
			if (_cachedListener.ContainsKey(eventType) == false)
				_cachedListener.Add(eventType, new List<Action<IEventMessage>>());

			if (_cachedListener[eventType].Contains(listener) == false)
			{
				// 同时记录到本地缓存和全局事件系统，便于 RemoveAllListener 一次性反注册。
				_cachedListener[eventType].Add(listener);
				UniEvent.AddListener(eventType, listener);
			}
			else
			{
				UniLogger.Warning($"Event listener is exist : {eventType}");
			}
		}

		/// <summary>
		/// 移除所有缓存的监听
		/// </summary>
		public void RemoveAllListener()
		{
			foreach (var pair in _cachedListener)
			{
				System.Type eventType = pair.Key;
				for (int i = 0; i < pair.Value.Count; i++)
				{
					// 按缓存记录逐个从全局事件系统移除。
					UniEvent.RemoveListener(eventType, pair.Value[i]);
				}
				pair.Value.Clear();
			}
			_cachedListener.Clear();
		}
	}
}
