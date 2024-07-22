using System;
using System.Net;
using System.Net.Sockets;
using DummyShared;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

public partial class DummyClient : Node, INetEventListener
{
    public static PackedScene playerScene = GD.Load<PackedScene>(
        "res://scenes/dummy_player_machine.tscn"
    );

    private NetManager client;
    private NetPeer server;
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;

    private DummyPlayerMachine playerMachine;

    [Export]
    private DummyInputHandler inputHandler;

    private int ticksElapsed;
    public bool joined { get; set; }

    public override void _Ready()
    {
        ticksElapsed = 0;
        joined = false;

        Connect();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (joined)
        {
            ticksElapsed += 1;
        }
    }

    public override void _Process(double delta)
    {
        client?.PollEvents();
    }

    public void PollEvents()
    {
        client?.PollEvents();
    }

    public void Connect()
    {
        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);

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
        // We want to send actions unreliably to get them to the server ASAP
        // Will suck if these are lost, so we probably need to continuously
        // send a buffer of all actions and the tick at which they occurred
        SendPacket(
            new PlayerActionPacket { action = action, clientTick = ticksElapsed },
            DeliveryMethod.Unreliable
        );
    }

    public void OnJoinAccept(JoinAcceptPacket packet)
    {
        GD.Print($"Join accepted by server (pid: {packet.pid})");
        ticksElapsed = packet.serverTicksElapsed;
        joined = true;

        playerMachine = playerScene.Instantiate() as DummyPlayerMachine;
        GetNode("../Players").AddChild(playerMachine);
        GetTree().Root.PrintTree();

        inputHandler.playerClient = this;
        inputHandler.playerMachine = playerMachine;
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
