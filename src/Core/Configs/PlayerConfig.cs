namespace Nyvorn.Configs;

public class PlayerConfig
{
    public StatsConfig Stats { get; set; } = new();
    public MovementConfig Movement { get; set; } = new();
    public AttackConfig Attack { get; set; } = new();
    public VisualConfig Visual { get; set; } = new();
}

public class StatsConfig
{
    public int Health { get; set; } = 100;
    public int maxHealth { get; set; } = 100;
    public int DarkEnergy { get; set; } = 10;
    public int MaxDarkEnery { get; set; } = 100;
}

public class MovementConfig
{
    public float WalkSpeed { get; set; } = 120f;
    public float SprintSpeed { get; set; } = 200f;
    public float Gravity { get; set; } = 1800f;
    public float JumpSpeed { get; set; } = 500f;
    public float CoyoteTime { get; set; } = 0.12f;
    public float JumpBuffer { get; set; } = 0.12f;
}

public class AttackConfig
{
    public int BaseDamage { get; set; } = 20;
    public float Duration { get; set; } = 0.22f;
    public int[] ImpactFrames { get; set; } = new[] { 2, 3 };
    public HitboxConfig Hitbox { get; set; } = new();
}

public class HitboxConfig
{
    public int ReachForward { get; set; } = 10;
    public int Height { get; set; } = 12;
    public int YOffset { get; set; } = 1;
}

public class VisualConfig
{
    public float IdleFrameRate { get; set; } = 0.15f;
    public float WalkFrameRate { get; set; } = 0.03f;
}
