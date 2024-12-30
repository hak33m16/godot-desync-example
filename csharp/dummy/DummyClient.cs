using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DummyShared;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;

public partial class DummyClient : Node, INetEventListener
{
    public static PackedScene playerScene = GD.Load<PackedScene>(
        "res://scenes/prefabs/player.tscn"
    );

    private NetManager client;
    private NetPeer server;
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;

    private Node2D playerSelf;
    private Dictionary<uint, Node2D> players = new();

    private float Speed = 2.0f;

    private bool Joined { get; set; } = false;
    private uint PeerId { get; set; }

    public override void _Ready()
    {
        Connect();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Joined)
        {
            return;
        }

        List<byte> tickActions = new();

        var body = playerSelf.GetNode<CharacterBody2D>("CharacterBody2D");

        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("walk_east"))
        {
            tickActions.Add((byte)PlayerAction.WalkEast);
            direction.X += 1;
        }
        if (Input.IsActionPressed("walk_west"))
        {
            tickActions.Add((byte)PlayerAction.WalkWest);
            direction.X -= 1;
        }
        if (Input.IsActionPressed("walk_north"))
        {
            tickActions.Add((byte)PlayerAction.WalkNorth);
            direction.Y -= 1;
        }
        if (Input.IsActionPressed("walk_south"))
        {
            tickActions.Add((byte)PlayerAction.WalkSouth);
            direction.Y += 1;
        }

        SendPacket(
            new PlayerActionsPacket { Actions = tickActions.ToArray() },
            DeliveryMethod.Unreliable
        );

        if (direction != Vector2.Zero)
        {
            direction = direction.Normalized();

            body.Velocity = direction * Speed;
            body.Position += body.Velocity;
        }
    }

    public override void _Process(double delta)
    {
        client?.PollEvents();
    }

    public void Connect()
    {
        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
        packetProcessor.SubscribeReusable<PlayerPositionsUpdatePacket>(OnPlayerPositionsUpdate);

        client = new NetManager(this) { AutoRecycle = true, };
        client.Start();
        GD.Print("Connecting to server");
        client.Connect("localhost", 12346, "");
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod)
        where T : class, new()
    {
        if (server != null)
        {
            writer.Reset();
            packetProcessor.Write(writer, packet);
            server.Send(writer, deliveryMethod);
        }
    }

    public void SendPlayerAction(PlayerAction action)
    {
        SendPacket(
            new PlayerActionPacket { Action = action },
            DeliveryMethod.Unreliable
        );
    }

    public void OnJoinAccept(JoinAcceptPacket packet)
    {
        GD.Print($"Join accepted by server (pid: {packet.pid})");
        Joined = true;

        playerSelf = playerScene.Instantiate<Node2D>();
        GetNode("../Players").AddChild(playerSelf);
        GetTree().Root.PrintTree();

        PeerId = packet.pid;
    }

    public void OnPlayerPositionsUpdate(PlayerPositionsUpdatePacket packet)
    {
        var positions = packet.ToVector2Array();

        var index = 0;
        foreach (var pid in packet.PlayerIds)
        {
            if (pid == PeerId)
            {
                // TODO: This is awful, we should not be incrementing this inside the loop
                index++;
                continue;
            }

            if (!players.ContainsKey(pid))
            {
                var newPlayer = playerScene.Instantiate<Node2D>();
                GetNode("../Players").AddChild(newPlayer);
                PrintTree();

                players[pid] = newPlayer;
                newPlayer.GetNode<CharacterBody2D>("CharacterBody2D").Position = positions[index];
            }
            else
            {
                players[pid].GetNode<CharacterBody2D>("CharacterBody2D").Position = positions[index];
            }

            index++;
        }
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        GD.Print("Connected to server");
        server = peer;
        SendPacket(new JoinPacket { }, DeliveryMethod.ReliableOrdered);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        throw new NotImplementedException();
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
        packetProcessor.ReadAllPackets(reader);
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
        throw new NotImplementedException();
    }
}
