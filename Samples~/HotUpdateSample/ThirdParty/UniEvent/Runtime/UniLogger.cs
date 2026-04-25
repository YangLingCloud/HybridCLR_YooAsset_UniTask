using System.Diagnostics;

namespace UniFramework.Event
{
	/// <summary>
	/// UniEvent 内部日志工具。
	/// </summary>
	internal static class UniLogger
	{
		/// <summary>
		/// 输出调试日志，仅在 DEBUG 符号存在时生效。
		/// </summary>
		[Conditional("DEBUG")]
		public static void Log(string info)
		{
			UnityEngine.Debug.Log(info);
		}

		/// <summary>
		/// 输出警告日志。
		/// </summary>
		public static void Warning(string info)
		{
			UnityEngine.Debug.LogWarning(info);
		}

		/// <summary>
		/// 输出错误日志。
		/// </summary>
		public static void Error(string info)
		{
			UnityEngine.Debug.LogError(info);
		}
	}
}
