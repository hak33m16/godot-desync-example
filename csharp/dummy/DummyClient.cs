using System;
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
    public static PackedScene playerMachineScene = GD.Load<PackedScene>(
        "res://scenes/dummy_player_machine.tscn"
    );

    private NetManager client;
    private NetPeer server;
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;

    private DummyPlayerMachine playerMachine;

    private Dictionary<uint, DummyPlayerMachine> players = new();

    // public readonly struct ActionsEntry
    // {
    //     public ActionsEntry(int tick, List<PlayerAction> actions)
    //     {
    //         Tick = tick;
    //         Actions = actions;
    //     }

    //     public int Tick { get; }
    //     public List<PlayerAction> Actions { get; }
    // }

    // public ActionsEntry[] ActionHistory = new ActionsEntry[60];
    // private ActionsEntry currentActions;

    [Export]
    private DummyInputHandler inputHandler;

    private int ticksElapsed = 0;
    public bool joined { get; set; }

    public override void _Ready()
    {
        // Engine.PhysicsTicksPerSecond = 20;

        // ticksElapsed = 0;
        joined = false;
        // currentActions = new ActionsEntry(ticksElapsed, new List<PlayerAction>());

        Connect();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (joined)
        {
            ticksElapsed += 1;
            // ActionHistory[ticksElapsed % 20] = currentActions;
            // currentActions = new ActionsEntry(ticksElapsed, new List<PlayerAction>());

            // if (ticksElapsed % 20 == 0)
            // {
            //     GD.Print(JsonConvert.SerializeObject(ActionHistory));
            // }
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
        packetProcessor.SubscribeReusable<SyncPacket>(OnSync);
        packetProcessor.SubscribeReusable<RemotePlayerJoinPacket>(OnRemotePlayerJoin);

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

    // public void SendPlayerAction(PlayerAction[] actions)
    // {
    //     // currentActions.Actions.Add(action);
    //     // We want to send actions unreliably to get them to the server ASAP
    //     // Will suck if these are lost, so we probably need to continuously
    //     // send a buffer of all actions and the tick at which they occurred
    //     SendPacket(
    //         new PlayerActionPacket { actions = actions },
    //         // new PlayerActionPacket { action = action, clientTick = ticksElapsed },
    //         DeliveryMethod.Unreliable
    //     );
    // }

    public void SendPlayerAction(PlayerAction action)
    {
        // currentActions.Actions.Add(action);
        // We want to send actions unreliably to get them to the server ASAP
        // Will suck if these are lost, so we probably need to continuously
        // send a buffer of all actions and the tick at which they occurred
        SendPacket(
            new PlayerActionPacket { action = action, clientTick = ticksElapsed },
            DeliveryMethod.Unreliable
        );
    }

    public void OnRemotePlayerJoin(RemotePlayerJoinPacket packet)
    {
        GD.Print($"Remote player joining (pid: {packet.pid})");

        var remoteMachine = playerMachineScene.Instantiate() as DummyPlayerMachine;
        GetNode("../Players").AddChild(remoteMachine);
        GetTree().Root.PrintTree();

        players.Add(packet.pid, remoteMachine);
    }

    public void OnJoinAccept(JoinAcceptPacket packet)
    {
        GD.Print($"Join accepted by server (pid: {packet.pid}) - server at tick {packet.serverTicksElapsed}");
        ticksElapsed = packet.serverTicksElapsed;
        joined = true;

        playerMachine = playerMachineScene.Instantiate() as DummyPlayerMachine;
        GetNode("../Players").AddChild(playerMachine);
        GetTree().Root.PrintTree();

        inputHandler.playerClient = this;
        inputHandler.playerMachine = playerMachine;
    }

    public void OnSync(SyncPacket packet)
    {
        ticksElapsed = packet.newClientTick;
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
