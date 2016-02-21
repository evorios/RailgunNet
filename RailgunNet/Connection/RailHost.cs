﻿/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
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
using System.Collections;
using System.Collections.Generic;

using CommonTools;

namespace Railgun
{
  /// <summary>
  /// Host is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public class RailHost : RailConnection
  {
    /// <summary>
    /// Fired after the host has been ticked normally.
    /// </summary>
    public event Action Updated;

    /// <summary>
    /// Fired when a new peer has been added to the host.
    /// </summary>
    public event RailPeerEvent PeerAdded;

    /// <summary>
    /// Fired when a peer has been removed from the host.
    /// </summary>
    public event RailPeerEvent PeerRemoved;

    private Dictionary<INetPeer, RailPeer> peers;

    public RailHost(params RailFactory[] factories) : base(factories)
    {
      this.peers = new Dictionary<INetPeer, RailPeer>();
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddPeer(INetPeer peer)
    {
      if (this.peers.ContainsKey(peer) == false)
      {
        RailPeer railPeer = new RailPeer(peer);
        this.peers.Add(peer, railPeer);

        if (this.PeerAdded != null)
          this.PeerAdded.Invoke(railPeer);
      }
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void RemovePeer(INetPeer peer)
    {
      if (this.peers.ContainsKey(peer))
      {
        RailPeer railPeer = this.peers[peer];
        this.peers.Remove(peer);

        if (this.PeerRemoved != null)
          this.PeerAdded.Invoke(railPeer);
      }
    }

    /// <summary>
    /// Updates all entites and dispatches a snapshot if applicable. Should
    /// be called once per game simulation tick (e.g. during Unity's 
    /// FixedUpdate pass).
    /// </summary>
    public void Update()
    {
      this.world.UpdateHost();

      if (this.Updated != null)
        this.Updated.Invoke();

      if (this.ShouldSend(this.world.Tick))
      {
        RailSnapshot snapshot = this.world.Snapshot();
        this.snapshots.Store(snapshot);
        this.Broadcast(snapshot);
      }
    }

    /// <summary>
    /// Queues a snapshot broadcast for each peer (handles delta-compression).
    /// </summary>
    internal void Broadcast(RailSnapshot snapshot)
    {
      foreach (RailPeer peer in this.peers.Values)
      {
        RailSnapshot basis;
        if (peer.LastAckedTick != RailClock.INVALID_TICK)
        {
          if (this.snapshots.TryGet(peer.LastAckedTick, out basis))
          {
            this.interpreter.EncodeSend(peer, snapshot, basis);
            return;
          }
        }

        this.interpreter.EncodeSend(peer, snapshot);
      }
    }

    /// <summary>
    /// Processes an incoming packet from a peer.
    /// </summary>
    private void Process(INetPeer peer, byte[] data)
    {
      // TODO
    }
  }
}
