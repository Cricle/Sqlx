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

        public ValueStringBuilder(int initialCapacity = 256)
        {
            _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            _pos = 0;
        }

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

        public int Capacity => _buffer.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            if (_pos >= _buffer.Length)
            {
                Grow();
            }
            _buffer[_pos++] = c;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _pos = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return new string(_buffer, 0, _pos);
        }

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
