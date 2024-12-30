// using System;
using System;
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
    }

    public class PlayerActionPacket
    {
        public PlayerAction Action { get; set; }
    }

    public class PlayerActionsPacket
    {
        public byte[] Actions { get; set; }
    }

    public class PlayerPositionsUpdatePacket
    {
        public uint[] PlayerIds { get; set; }
        public float[] PositionsX { get; set; }
        public float[] PositionsY { get; set; }

        public static PlayerPositionsUpdatePacket FromVector2Array(uint[] playerIds, Godot.Vector2[] positions)
        {
            var packet = new PlayerPositionsUpdatePacket
            {
                PlayerIds = playerIds,
                PositionsX = new float[positions.Length],
                PositionsY = new float[positions.Length]
            };

            for (int i = 0; i < positions.Length; i++)
            {
                packet.PositionsX[i] = positions[i].X;
                packet.PositionsY[i] = positions[i].Y;
            }

            return packet;
        }

        public Godot.Vector2[] ToVector2Array()
        {
            var positions = new Godot.Vector2[PositionsX.Length];
            for (int i = 0; i < PositionsX.Length; i++)
            {
                positions[i] = new Godot.Vector2(PositionsX[i], PositionsY[i]);
            }

            return positions;
        }
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
