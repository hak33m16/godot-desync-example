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
    }
}
