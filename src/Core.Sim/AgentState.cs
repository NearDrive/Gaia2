using System.Numerics;

namespace Core.Sim;

public sealed class AgentState
{
    private float _thirst01;
    private float _secondsAtMaxThirst;

    public AgentState(Vector2 position)
    {
        Position = position;
        LastVision = Array.Empty<float>();
        _thirst01 = 0f;
        _secondsAtMaxThirst = 0f;
        IsAlive = true;
    }

    public Vector2 Position { get; private set; }

    public float[] LastVision { get; private set; }

    public float Thirst01 => _thirst01;

    public bool IsAlive { get; private set; }

    public bool TryApplyDelta(GridWorld world, Vector2 delta)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Vector2 next = Position + delta;
        if (world.IsBlocked(next))
        {
            return false;
        }

        Position = next;
        return true;
    }

    public void UpdateThirst(float dt, float thirstRatePerSecond, float deathGraceSeconds)
    {
        if (!IsAlive)
        {
            return;
        }

        _thirst01 = Math.Clamp(_thirst01 + (thirstRatePerSecond * dt), 0f, 1f);

        if (_thirst01 >= 1f)
        {
            _secondsAtMaxThirst += dt;
        }
        else
        {
            _secondsAtMaxThirst = 0f;
        }

        if (_secondsAtMaxThirst >= deathGraceSeconds)
        {
            IsAlive = false;
        }
    }

    public void ApplyDrink(float amount)
    {
        if (!IsAlive)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        _thirst01 = Math.Max(0f, _thirst01 - amount);
        if (_thirst01 < 1f)
        {
            _secondsAtMaxThirst = 0f;
        }
    }

    public void SetThirst01(float value)
    {
        _thirst01 = Math.Clamp(value, 0f, 1f);
        if (_thirst01 < 1f)
        {
            _secondsAtMaxThirst = 0f;
        }
    }

    public void UpdateVision(float[] vision)
    {
        LastVision = vision ?? throw new ArgumentNullException(nameof(vision));
    }
}
