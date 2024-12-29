// using System;
using System.Numerics;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

namespace DummyShared
{
    public enum PlayerAction : byte
    {
        Attack,
        WalkNorth,
        WalkEast,
        WalkSouth,
        WalkWest,
    }

    public class JoinPacket { }

    public class JoinAcceptPacket
    {
        public uint pid { get; set; }
        public int serverTicksElapsed { get; set; }
        // public uint[] playerIds { get; set; }
        // public Godot.Vector2[] playerPositions { get; set; }
    }

    public class PlayerActionPacket
    {
        // public PlayerAction[] actions { get; set; }
        public PlayerAction action { get; set; }
        public int clientTick { get; set; }
    }

    public class RemotePlayerJoinPacket
    {
        public uint pid { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    public class SyncPacket
    {
        public int serverTicksElapsed { get; set; }
        public int newClientTick { get; set; }
    }
}
