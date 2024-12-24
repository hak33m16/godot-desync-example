using System;
using DummyShared;
using Godot;

public partial class DummyPlayerMachine : Node
{
    public static PackedScene playerScene = GD.Load<PackedScene>(
        "res://scenes/prefabs/dummy_player.tscn"
    );

    public const int ATTACK_DURATION_TICKS = 120;
    public const int ATTACK_COOLDOWN_TICKS = 120;

    public enum PlayerState
    {
        Idle,
        Attacking,
        Walking,
    }

    public PlayerState state = PlayerState.Idle;

    private bool input_attemptAttack = false;
    private int attackDurationRemaining = 0;
    private int global_attackCooldownRemaining = 0;

    private float X { get; set; } = 0;
    private float Y { get; set; } = 0;
    private float Speed { get; set; } = 4.0f;

    [Export]
    private Node player;

    public DummyPlayerMachine()
    {
        player = playerScene.Instantiate();
        AddChild(player);
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (state)
        {
            case PlayerState.Idle:
                if (input_attemptAttack)
                {
                    GD.Print(
                        DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                            + " Attack attempted while idle!"
                    );
                    if (global_attackCooldownRemaining == 0)
                    {
                        GD.Print(
                            DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                                + " Player is not in cooldown, beginning attack!"
                        );
                        state = PlayerState.Attacking;
                    }
                    else
                    {
                        GD.Print(
                            DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                                + " Couldn't attack because there's still some cooldown time remaining! ("
                                + global_attackCooldownRemaining
                                + ")"
                        );

                        // Leaving this out almost turns this into a buffer...
                        input_attemptAttack = false;
                    }
                }

                break;
            case PlayerState.Attacking:
                if (
                    input_attemptAttack
                    && attackDurationRemaining == 0
                    && global_attackCooldownRemaining == 0
                )
                {
                    input_attemptAttack = false;
                    attackDurationRemaining = ATTACK_DURATION_TICKS;
                }
                else if (attackDurationRemaining > 0)
                {
                    attackDurationRemaining -= 1;

                    if (input_attemptAttack)
                    {
                        GD.Print(
                            DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                                + " Can't attack while still in the middle of attacking! ("
                                + attackDurationRemaining
                                + ")"
                        );

                        // Leaving this out almost turns this into a buffer...
                        input_attemptAttack = false;
                    }
                }
                else if (attackDurationRemaining == 0)
                {
                    GD.Print(
                        DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")
                            + " Done attacking so transitioning back to idle! ("
                            + attackDurationRemaining
                            + ")"
                    );

                    global_attackCooldownRemaining = ATTACK_COOLDOWN_TICKS;
                    state = PlayerState.Idle;
                }

                break;
        }

        if (global_attackCooldownRemaining > 0)
        {
            global_attackCooldownRemaining -= 1;
        }
    }

    public override void _Process(double delta)
    {
        player.GetNode<Node2D>("Node2D").Position = new Vector2(X, Y);
    }

    public void HandleAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Attack:
                input_attemptAttack = true;
                break;
            case PlayerAction.WalkNorth:
                Y -= Speed;
                break;
            case PlayerAction.WalkEast:
                X += Speed;
                break;
            case PlayerAction.WalkSouth:
                Y += Speed;
                break;
            case PlayerAction.WalkWest:
                X -= Speed;
                break;
        }
    }
}
