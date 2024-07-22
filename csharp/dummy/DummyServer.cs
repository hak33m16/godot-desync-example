using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DummyShared;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

public partial class DummyServer : Node, INetEventListener
{
    public class ServerPlayer
    {
        public NetPeer peer;
        public DummyPlayerMachine playerNode;
    }

    public static PackedScene playerScene = GD.Load<PackedScene>(
        "res://scenes/dummy_player_machine.tscn"
    );

    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private Dictionary<uint, ServerPlayer> players = new();
    private NetManager server;

    private int ticksElapsed = 0;

    public override void _Ready()
    {
        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
        packetProcessor.SubscribeReusable<PlayerActionPacket, NetPeer>(OnPlayerActionReceived);

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

        DummyPlayerMachine playerInstance = playerScene.Instantiate() as DummyPlayerMachine;
        AddChild(playerInstance);
        PrintTree();

        players[(uint)peer.Id] = new ServerPlayer { peer = peer, playerNode = playerInstance };

        SendPacket(
            new JoinAcceptPacket { pid = (uint)peer.Id },
            peer,
            DeliveryMethod.ReliableOrdered
        );
    }

    public void OnPlayerActionReceived(PlayerActionPacket packet, NetPeer peer)
    {
        // GD.Print($"Received player action {packet.action} from (pid: {(uint)peer.Id})");
        var player = players[(uint)peer.Id];

        player.playerNode.HandleAction(packet.action);
    }

    public override void _PhysicsProcess(double delta)
    {
        server.PollEvents();

        ticksElapsed += 1;
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        GD.Print("Peer connected: " + peer.Id);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        GD.Print("Peer disconnected: " + peer.Id);
        // Probably want to free the node, no?
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
