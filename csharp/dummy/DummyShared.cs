using System;
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
    }

    public class PlayerActionPacket
    {
        public PlayerAction action { get; set; }
        public int clientTick { get; set; }
    }
}
