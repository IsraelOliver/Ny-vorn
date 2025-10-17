using System;
using Microsoft.Xna.Framework;

namespace Nyvorn;

public interface IDamageable
{
    bool IsAlive { get; }
    Rectangle Bounds { get; }            // útil p/ validação extra
    Vector2 Position { get; }            // centro/pos para direção
    void ApplyHit(in HitInfo hit);       // recebe o pacote
}

public enum Faction { Player, Enemy, Neutral }

public readonly struct HitInfo
{
    public readonly int Damage;
    public readonly int DirX;            // -1, 0, +1 (em relação ao atacante)
    public readonly float KnockbackX;
    public readonly float KnockbackY;
    public readonly float Stun;          // opcional: em segundos
    public readonly Faction Source;      // de quem veio (FF off/on)
    public readonly string Tag;          // ex: "Slash", "Contact", "Projectile"

    public HitInfo(int dmg, int dirX, float kbX, float kbY, float stun, Faction src, string tag)
    {
        Damage = dmg; DirX = Math.Sign(dirX);
        KnockbackX = kbX; KnockbackY = kbY; Stun = stun;
        Source = src; Tag = tag;
    }
}

public sealed class DamageEvent
{
    public IDamageable Target { get; }
    public HitInfo Hit { get; }
    public DamageEvent(IDamageable target, in HitInfo hit) { Target = target; Hit = hit; }
}