// -----------------------------------------------------------------------
// <copyright file="ValueStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Annotations
{
    /// <summary>
    /// 高性能的值类型字符串构建器，使用ArrayPool避免堆分配，无锁设计。
    /// </summary>
    public ref struct ValueStringBuilder
    {
        private char[] _buffer;
        private int _pos;

        /// <summary>
        /// 初始化ValueStringBuilder的新实例。
        /// </summary>
        /// <param name="initialCapacity">初始容量，默认为256字符。</param>
        public ValueStringBuilder(int initialCapacity = 256)
        {
            _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            _pos = 0;
        }

        /// <summary>
        /// 获取或设置当前字符串的长度。
        /// </summary>
        public int Length
        {
            get => _pos;
            set
            {
                if (value < 0 || value > _buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _pos = value;
            }
        }

        /// <summary>
        /// 获取当前缓冲区的容量。
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// 向字符串构建器追加单个字符。
        /// </summary>
        /// <param name="c">要追加的字符。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            if (_pos >= _buffer.Length)
            {
                Grow();
            }
            _buffer[_pos++] = c;
        }

        /// <summary>
        /// 向字符串构建器追加字符串。
        /// </summary>
        /// <param name="s">要追加的字符串。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string s)
        {
            if (string.IsNullOrEmpty(s)) return;

            if (_pos + s.Length > _buffer.Length)
            {
                Grow(s.Length);
            }

            s.CopyTo(0, _buffer, _pos, s.Length);
            _pos += s.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int additionalLength = 1)
        {
            int newCapacity = Math.Max(_buffer.Length * 2, _pos + additionalLength);
            var newBuffer = ArrayPool<char>.Shared.Rent(newCapacity);
            Array.Copy(_buffer, 0, newBuffer, 0, _pos);

            // 归还旧的buffer
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        /// <summary>
        /// 清空字符串构建器的内容。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _pos = 0;
        }

        /// <summary>
        /// 将当前的字符串构建器转换为字符串。
        /// </summary>
        /// <returns>构建的字符串。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return new string(_buffer, 0, _pos);
        }

        /// <summary>
        /// 释放资源，将缓冲区返回到ArrayPool。
        /// </summary>
        public void Dispose()
        {
            // 归还buffer到ArrayPool
            if (_buffer != null)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = null!;
            }
            _pos = 0;
        }
    }
}
