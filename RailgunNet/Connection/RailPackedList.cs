﻿/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;
using RailgunNet.System.Encoding;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection
{
    internal class RailPackedListS2C<T>
        where T : IRailPoolable<T>
    {
        public RailPackedListS2C()
        {
#if CLIENT
            received = new List<T>();
#endif
#if SERVER
            pending = new List<T>();
            sent = new List<T>();
#endif
        }

        public void Clear()
        {
#if CLIENT
            // We don't free the received values as they will be passed elsewhere
            received.Clear();
#endif
#if SERVER
            // Everything in sent is also in pending, so only free pending
            foreach (T value in pending)
                RailPool.Free(value);
            pending.Clear();
            sent.Clear();
#endif
        }

#if CLIENT
        public void Decode(
            RailBitBuffer buffer,
            Func<T> decode)
        {
            IEnumerable<T> decoded = buffer.UnpackAll(decode);
            foreach (T delta in decoded)
                received.Add(delta);
        }
#endif
#if CLIENT
        public IEnumerable<T> Received => received;
        private readonly List<T> received;
#endif
#if SERVER
        public IEnumerable<T> Pending => pending;
        public IEnumerable<T> Sent => sent;
        private readonly List<T> pending;
        private readonly List<T> sent;
#endif
#if SERVER
        public void AddPending(T value)
        {
            pending.Add(value);
        }

        public void AddPending(IEnumerable<T> values)
        {
            pending.AddRange(values);
        }

        public void Encode(
            RailBitBuffer buffer,
            int maxTotalSize,
            int maxIndividualSize,
            Action<T> encode)
        {
            buffer.PackToSize(
                maxTotalSize,
                maxIndividualSize,
                pending,
                encode,
                val => sent.Add(val));
        }
#endif
    }

    internal class RailPackedListC2S<T>
        where T : IRailPoolable<T>
    {
        public RailPackedListC2S()
        {
#if SERVER
            received = new List<T>();
#endif
#if CLIENT
            pending = new List<T>();
            sent = new List<T>();
#endif
        }

        public void Clear()
        {
#if SERVER
            // We don't free the received values as they will be passed elsewhere
            received.Clear();
#endif
#if CLIENT
            // Everything in sent is also in pending, so only free pending
            foreach (T value in pending)
                RailPool.Free(value);
            pending.Clear();
            sent.Clear();
#endif
        }

#if SERVER
        public void Decode(
            RailBitBuffer buffer,
            Func<T> decode)
        {
            IEnumerable<T> decoded = buffer.UnpackAll(decode);
            foreach (T delta in decoded)
                received.Add(delta);
        }
#endif
#if SERVER
        public IEnumerable<T> Received => received;
        private readonly List<T> received;
#endif
#if CLIENT
        public IEnumerable<T> Pending => pending;
        public IEnumerable<T> Sent => sent;
        private readonly List<T> pending;
        private readonly List<T> sent;
#endif
#if CLIENT
        public void AddPending(T value)
        {
            pending.Add(value);
        }

        public void AddPending(IEnumerable<T> values)
        {
            pending.AddRange(values);
        }

        public void Encode(
            RailBitBuffer buffer,
            int maxTotalSize,
            int maxIndividualSize,
            Action<T> encode)
        {
            buffer.PackToSize(
                maxTotalSize,
                maxIndividualSize,
                pending,
                encode,
                val => sent.Add(val));
        }
#endif
    }
}