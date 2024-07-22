using System;
using DummyShared;
using Godot;

public partial class DummyPlayerMachine : Node
{
    public const int ATTACK_DURATION_TICKS = 120;
    public const int ATTACK_COOLDOWN_TICKS = 120;

    public enum PlayerState
    {
        Idle,
        Attacking,
    }

    public PlayerState state = PlayerState.Idle;

    private bool input_attemptAttack = false;
    private int attackDurationRemaining = 0;
    private int global_attackCooldownRemaining = 0;

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

    public void HandleAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Attack:
                input_attemptAttack = true;
                break;
        }
    }
}
