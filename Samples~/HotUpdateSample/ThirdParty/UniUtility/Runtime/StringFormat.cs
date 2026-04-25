using System;
using System.Text;

namespace UniFramework.Utility
{
	/// <summary>
	/// 基于线程静态 StringBuilder 的字符串格式化工具，减少频繁格式化产生的临时分配。
	/// </summary>
	public static class StringFormat
	{
		[ThreadStatic]
		private static StringBuilder _cacheBuilder = new StringBuilder(1024);

		/// <summary>
		/// 格式化一个参数的字符串。
		/// </summary>
		public static string Format(string format, object arg0)
		{
			if (string.IsNullOrEmpty(format))
				throw new ArgumentNullException();

			// 复用线程本地 StringBuilder，避免每次 Format 都创建新的构建器。
			_cacheBuilder.Length = 0;
			_cacheBuilder.AppendFormat(format, arg0);
			return _cacheBuilder.ToString();
		}

		/// <summary>
		/// 格式化两个参数的字符串。
		/// </summary>
		public static string Format(string format, object arg0, object arg1)
		{
			if (string.IsNullOrEmpty(format))
				throw new ArgumentNullException();

			// 每次格式化前清空缓存，确保不会残留上一次内容。
			_cacheBuilder.Length = 0;
			_cacheBuilder.AppendFormat(format, arg0, arg1);
			return _cacheBuilder.ToString();
		}

		/// <summary>
		/// 格式化三个参数的字符串。
		/// </summary>
		public static string Format(string format, object arg0, object arg1, object arg2)
		{
			if (string.IsNullOrEmpty(format))
				throw new ArgumentNullException();

			_cacheBuilder.Length = 0;
			_cacheBuilder.AppendFormat(format, arg0, arg1, arg2);
			return _cacheBuilder.ToString();
		}

		/// <summary>
		/// 格式化可变参数字符串。
		/// </summary>
		public static string Format(string format, params object[] args)
		{
			if (string.IsNullOrEmpty(format))
				throw new ArgumentNullException();

			if (args == null)
				throw new ArgumentNullException();

			_cacheBuilder.Length = 0;
			_cacheBuilder.AppendFormat(format, args);
			return _cacheBuilder.ToString();
		}
	}
}
