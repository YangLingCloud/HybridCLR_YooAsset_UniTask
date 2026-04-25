using System;

namespace UniFramework.Utility
{
    /// <summary>
    /// 32 位掩码工具，用于对 int 中的单个位进行开关、翻转和测试。
    /// </summary>
    internal struct BitMask32
    {
        private int _mask;

        /// <summary>
        /// 将 BitMask32 隐式转换为 int 原始掩码值。
        /// </summary>
        public static implicit operator int(BitMask32 mask) { return mask._mask; }

        /// <summary>
        /// 将 int 原始掩码值隐式转换为 BitMask32。
        /// </summary>
        public static implicit operator BitMask32(int mask) { return new BitMask32(mask); }

        /// <summary>
        /// 使用原始掩码值创建 32 位掩码。
        /// </summary>
        public BitMask32(int mask)
        {
            _mask = mask;
        }

        /// <summary>
        /// 打开位
        /// </summary>
        public void Open(int bit)
        {
			if (bit < 0 || bit > 31)
				throw new ArgumentOutOfRangeException();
            else
            {
                // 使用或运算把目标位设置为 1。
                _mask |= 1 << bit;
            }
        }

        /// <summary>
        /// 关闭位
        /// </summary>
        public void Close(int bit)
        {
            if (bit < 0 || bit > 31)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 取反目标位掩码后与当前值相与，将目标位清零。
                _mask &= ~(1 << bit);
            }
        }

        /// <summary>
        /// 位取反
        /// </summary>
        public void Reverse(int bit)
        {
            if (bit < 0 || bit > 31)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 使用异或运算翻转目标位。
                _mask ^= 1 << bit;
            }
        }

		/// <summary>
		/// 所有位取反
		/// </summary>
		public void Inverse()
		{
			_mask = ~_mask;
		}

		/// <summary>
		/// 比对位值
		/// </summary>
		public bool Test(int bit)
        {
            if (bit < 0 || bit > 31)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 与目标位掩码相与后非零，说明该位已开启。
				return (_mask & (1 << bit)) != 0;
            }
        }
    }
}
