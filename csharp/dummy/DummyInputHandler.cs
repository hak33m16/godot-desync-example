using System;
using Godot;

public partial class DummyInputHandler : Node
{
    public DummyClient playerClient { get; set; }
    public DummyPlayerMachine playerMachine { get; set; }

    // Handle input as soon as it's received
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("attack_1"))
        {
            playerMachine.HandleAction(DummyShared.PlayerAction.Attack);
            playerClient.SendPlayerAction(DummyShared.PlayerAction.Attack);
        }

        if (Input.IsActionPressed("walk_north"))
        {
            playerMachine.HandleAction(DummyShared.PlayerAction.WalkNorth);
            playerClient.SendPlayerAction(DummyShared.PlayerAction.WalkNorth);
        }

        if (Input.IsActionPressed("walk_east"))
        {
            playerMachine.HandleAction(DummyShared.PlayerAction.WalkEast);
            playerClient.SendPlayerAction(DummyShared.PlayerAction.WalkEast);
        }

        if (Input.IsActionPressed("walk_south"))
        {
            playerMachine.HandleAction(DummyShared.PlayerAction.WalkSouth);
            playerClient.SendPlayerAction(DummyShared.PlayerAction.WalkSouth);
        }

        if (Input.IsActionPressed("walk_west"))
        {
            playerMachine.HandleAction(DummyShared.PlayerAction.WalkWest);
            playerClient.SendPlayerAction(DummyShared.PlayerAction.WalkWest);
        }
    }
}
