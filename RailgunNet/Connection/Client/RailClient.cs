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

using RailgunNet.Connection.Server;
using RailgunNet.Connection.Traffic;
using RailgunNet.Factory;
using RailgunNet.Logic;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Types;
using RailgunNet.Util;
using RailgunNet.Util.Debug;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Connection.Client
{
    [OnlyIn(Component.Client)]
    public class RailClient
        : RailConnection
    {
        /// <summary>
        ///     The local simulation tick, used for commands
        /// </summary>
        private Tick localTick;

        /// <summary>
        ///     The peer for our connection to the server.
        /// </summary>
        private RailClientPeer serverPeer;

        public RailClient(RailRegistry registry)
            : base(registry)
        {
            serverPeer = null;
            localTick = Tick.START;
            Room = null;
        }

        /// <summary>
        ///     The client's room instance. TODO: Multiple rooms?
        /// </summary>
        private new RailClientRoom Room { get; set; }

        public void StartRoom()
        {
            Room = new RailClientRoom(resource, this);
            SetRoom(Room, Tick.INVALID);
        }

        /// <summary>
        ///     Sets the current server peer.
        /// </summary>
        public void SetPeer(IRailNetPeer netPeer)
        {
            if (netPeer == null)
            {
                if (serverPeer != null)
                {
                    serverPeer.PacketReceived -= OnPacketReceived;
                    serverPeer.EventReceived -= OnEventReceived;
                }

                serverPeer = null;
            }
            else
            {
                RailDebug.Assert(serverPeer == null, "Overwriting peer");
                serverPeer =
                    new RailClientPeer(resource, netPeer, Interpreter);
                serverPeer.PacketReceived += OnPacketReceived;
                serverPeer.EventReceived += OnEventReceived;
            }
        }

        public override void Update()
        {
            if (serverPeer != null)
            {
                DoStart();
                serverPeer.Update(localTick);

                if (Room != null)
                {
                    Room.ClientUpdate(
                        localTick,
                        serverPeer.EstimatedRemoteTick);

                    int sendRate = RailConfig.CLIENT_SEND_RATE;
                    if (localTick.IsSendTick(sendRate))
                        serverPeer.SendPacket(
                            localTick,
                            Room.LocalEntities);

                    localTick++;
                }
            }
        }

        /// <summary>
        ///     Queues an event to sent to the server.
        /// </summary>
        public void RaiseEvent(
            RailEvent evnt,
            ushort attempts = 3,
            bool freeWhenDone = true)
        {
            RailDebug.Assert(serverPeer != null);
            serverPeer?.RaiseEvent(evnt, attempts, freeWhenDone);
        }

        private void OnPacketReceived(IRailServerPacket packet)
        {
            if (Room == null)
                foreach (RailStateDelta delta in packet.Deltas)
                    RailPool.Free(delta);
            else
                foreach (RailStateDelta delta in packet.Deltas)
                    if (Room.ProcessDelta(delta) == false)
                        RailPool.Free(delta);
        }
    }
}