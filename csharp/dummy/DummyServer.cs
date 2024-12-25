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
    public readonly struct ActionsEntry
    {
        public ActionsEntry(int tick, List<PlayerAction> actions)
        {
            Tick = tick;
            Actions = actions;
        }

        public int Tick { get; }
        public List<PlayerAction> Actions { get; }
    }

    public class ServerPlayer
    {
        public NetPeer peer;
        public DummyPlayerMachine playerNode;
        public ActionsEntry[] ActionHistory = new ActionsEntry[60];
        public ActionsEntry CurrentActions;
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

        // Soooo we probably need to change the action packet to include the action history info from the client
        //
        // But once it includes it, what are we gonna do?
        //
        // In general, we want the client to be just far enough behind us that by the time their packet gets
        // to the server, it's perfectly ready to be executed
        //
        // Obviously that can't happen in reality
        //
        // So say the client is at tick 100, client presses W, this generates an action at
        // tick 100 containing W. By the time it arrives to the server, the server is just now
        // about to process tick 100. That's the best case scenario here.
        //
        // The packet could arrive after the server time of 100, which would mean we have to ignore
        // the contents of it. The server will never go back in time. Sorry, not sorry, you missed
        // your chance to tell us what you wanted.
        // So here we're saying we never ever want the client to get too far behind the server. If they're
        // sending us information from the past, either their clock has drifted, in which case we need to
        // resynchronize it, or their hardware can't keep up.
        //
        // The packet could also arrive before the server reaches 100. How far before this should
        // we accept an incoming packet? Presumably this locks the player into performing the actions
        // that are within that packet at that tick.
        // This does have some interesting implications however, because it means the client is actually
        // processing too far into the future. This can also happen from clock drift, in which case we would
        // also need to resynchronize the clients tick clock. Without doing this, the client may eventually
        // become permanently out of sync/too far ahead from the server, in which case we'd be stuck rejecting
        // packets because they're much too far in the future.
        //
        // Overall, if packets arrive before us, too little too late. If they arrive after us, we'll
        // queue them up, but only if they're like just about to happen.
        //
        // And none of these even talks about state reconciliation...
        player.CurrentActions.Actions.Add(packet.action);
        player.playerNode.HandleAction(packet.action);
    }

    public override void _PhysicsProcess(double delta)
    {
        server.PollEvents();

        ticksElapsed += 1;
        foreach (ServerPlayer player in players.Values)
        {
            player.ActionHistory[ticksElapsed % 20] = player.CurrentActions;
            player.CurrentActions = new ActionsEntry(ticksElapsed, new List<PlayerAction>());
        }
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
