using System;

namespace UniFramework.Utility
{
    /// <summary>
    /// 64 位掩码工具，用于对 long 中的单个位进行开关、翻转和测试。
    /// </summary>
    internal struct BitMask64
    {
        private long _mask;

        /// <summary>
        /// 将 BitMask64 隐式转换为 long 原始掩码值。
        /// </summary>
        public static implicit operator long(BitMask64 mask) { return mask._mask; }

        /// <summary>
        /// 将 long 原始掩码值隐式转换为 BitMask64。
        /// </summary>
        public static implicit operator BitMask64(long mask) { return new BitMask64(mask); }
        
        /// <summary>
        /// 使用原始掩码值创建 64 位掩码。
        /// </summary>
        public BitMask64(long mask)
        {
            _mask = mask;
        }

        /// <summary>
        /// 打开位
        /// </summary>
        public void Open(int bit)
        {
			if (bit < 0 || bit > 63)
				throw new ArgumentOutOfRangeException();
            else
            {
                // 使用或运算把目标位设置为 1。
                _mask |= 1L << bit;
            }
        }

        /// <summary>
        /// 关闭位
        /// </summary>
        public void Close(int bit)
        {
            if (bit < 0 || bit > 63)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 取反目标位掩码后与当前值相与，将目标位清零。
                _mask &= ~(1L << bit);
            }
        }

        /// <summary>
        /// 位取反
        /// </summary>
        public void Reverse(int bit)
        {
            if (bit < 0 || bit > 63)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 使用异或运算翻转目标位。
                _mask ^= 1L << bit;
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
            if (bit < 0 || bit > 63)
				throw new ArgumentOutOfRangeException();
			else
            {
                // 与目标位掩码相与后非零，说明该位已开启。
				return (_mask & (1L << bit)) != 0;
            }
        }
    }
}
