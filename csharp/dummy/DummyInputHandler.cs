using System;
using System.Collections.Generic;
using DummyShared;
using Godot;

public partial class DummyInputHandler : Node
{
    public DummyClient playerClient { get; set; }
    public DummyPlayerMachine playerMachine { get; set; }
    public List<PlayerAction> ActionQueue { get; set; } = new List<PlayerAction>();

    // We should probably be handling input as soon as it's received via _Process
    // But then applying it during the next tick within _PhysicsProcess
    public override void _PhysicsProcess(double delta)
    {
        ActionQueue.Clear();

        if (Input.IsActionJustPressed("attack_1"))
        {
            playerMachine.HandleAction(PlayerAction.Attack);
            ActionQueue.Add(PlayerAction.Attack);
        }

        if (Input.IsActionPressed("walk_north"))
        {
            playerMachine.HandleAction(PlayerAction.WalkNorth);
            ActionQueue.Add(PlayerAction.WalkNorth);
        }

        if (Input.IsActionPressed("walk_east"))
        {
            playerMachine.HandleAction(PlayerAction.WalkEast);
            ActionQueue.Add(PlayerAction.WalkEast);
        }

        if (Input.IsActionPressed("walk_south"))
        {
            playerMachine.HandleAction(PlayerAction.WalkSouth);
            ActionQueue.Add(PlayerAction.WalkSouth);
        }

        if (Input.IsActionPressed("walk_west"))
        {
            playerMachine.HandleAction(PlayerAction.WalkWest);
            ActionQueue.Add(PlayerAction.WalkWest);
        }

        // This is obviously terrible. Just send an array of the actions
        foreach (PlayerAction action in ActionQueue)
        {
            playerClient.SendPlayerAction(action);
        }
    }
}
