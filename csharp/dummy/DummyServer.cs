using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using DummyShared;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;

public partial class DummyServer : Node, INetEventListener
{

    public class ServerPlayer
    {
        public NetPeer peer;
        public Node2D playerNode;
    }

    public static PackedScene playerScene = GD.Load<PackedScene>(
        "res://scenes/prefabs/player.tscn"
    );

    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private Dictionary<uint, ServerPlayer> players = new();
    private NetManager server;

    private int TicksElapsed { get; set; } = 0;

    private float Speed = 5.0f;

    public override void _Ready()
    {
        Engine.PhysicsTicksPerSecond = 20;

        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
        // packetProcessor.SubscribeReusable<PlayerActionPacket, NetPeer>(OnPlayerActionReceived);
        packetProcessor.SubscribeReusable<PlayerActionsPacket, NetPeer>(OnPlayerActionsReceived);
        // packetProcessor.SubscribeReusable<PlayerPositionsPacket, NetPeer>(OnPlayerActionsReceived);

        server = new NetManager(this) { AutoRecycle = true, };
        GD.Print("Starting server");
        server.Start(12346);
    }

    public void SendPacket<T>(T packet, NetPeer peer, DeliveryMethod deliveryMethod)
        where T : class, new()
    {
        if (peer != null)
        {
            writer.Reset();
            packetProcessor.Write(writer, packet);
            peer.Send(writer, deliveryMethod);
        }
    }

    public void OnJoinReceived(JoinPacket packet, NetPeer peer)
    {
        GD.Print($"Received join from (pid: {(uint)peer.Id})");

        var playerInstance = playerScene.Instantiate<Node2D>();
        AddChild(playerInstance);
        PrintTree();

        SendPacket(
            new JoinAcceptPacket { pid = (uint)peer.Id },
            peer,
            DeliveryMethod.ReliableOrdered
        );

        players[(uint)peer.Id] = new ServerPlayer { peer = peer, playerNode = playerInstance };

        BroadcastPlayerPositions();
    }

    public void BroadcastPlayerPositions()
    {
        List<uint> pids = new();
        List<Vector2> positions = new();
        foreach (var entry in players)
        {
            pids.Add(entry.Key);
            positions.Add(entry.Value.playerNode.GetNode<CharacterBody2D>("CharacterBody2D").Position);
        }

        GD.Print($"pids to broadcast: {JsonConvert.SerializeObject(pids)}");
        GD.Print($"positions to broadcast: {JsonConvert.SerializeObject(positions)}");

        var packet = PlayerPositionsUpdatePacket.FromVector2Array(pids.ToArray(), positions.ToArray());
        foreach (var entry in players)
        {
            SendPacket(packet, entry.Value.peer, DeliveryMethod.Unreliable);
        }
    }

    public void OnPlayerActionsReceived(PlayerActionsPacket packet, NetPeer peer)
    {
        var body = players[(uint)peer.Id].playerNode.GetNode<CharacterBody2D>("CharacterBody2D");

        Vector2 direction = Vector2.Zero;
        if (packet.Actions.Contains((byte)PlayerAction.WalkEast))
        {
            direction.X += 1;
        }
        if (packet.Actions.Contains((byte)PlayerAction.WalkWest))
        {
            direction.X -= 1;
        }
        if (packet.Actions.Contains((byte)PlayerAction.WalkNorth))
        {
            direction.Y -= 1;
        }
        if (packet.Actions.Contains((byte)PlayerAction.WalkSouth))
        {
            direction.Y += 1;
        }

        if (direction != Vector2.Zero)
        {
            direction = direction.Normalized();

            body.Velocity = direction * Speed;
            body.Position += body.Velocity;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (server == null)
            return;

        server.PollEvents();

        // if (TicksElapsed % 5 == 0)
        // {
        BroadcastPlayerPositions();
        // }

        TicksElapsed++;
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        GD.Print("Peer connected: " + peer.Id);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        GD.Print("Peer disconnected: " + peer.Id);

        ServerPlayer player = players.GetValueOrDefault((uint)peer.Id);
        player.playerNode.QueueFree();

        players.Remove((uint)peer.Id);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channelNumber,
        DeliveryMethod deliveryMethod
    )
    {
        packetProcessor.ReadAllPackets(reader, peer);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(
        IPEndPoint remoteEndPoint,
        NetPacketReader reader,
        UnconnectedMessageType messageType
    )
    {
        throw new NotImplementedException();
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        GD.Print($"Incoming connection from {request.RemoteEndPoint.ToString()}");
        request.Accept();
    }
}
