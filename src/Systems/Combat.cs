using System;
using Microsoft.Xna.Framework;

namespace Nyvorn;

public interface IDamageable
{
    bool IsAlive { get; }
    Rectangle Bounds { get; }            
    Vector2 Position { get; }            
    void ApplyHit(in HitInfo hit);       
}

public enum Faction { Player, Enemy, Neutral }

public readonly struct HitInfo
{
    public readonly int Damage;
    public readonly int DirX;           
    public readonly float KnockbackX;
    public readonly float KnockbackY;
    public readonly float Stun;          
    public readonly Faction Source;      
    public readonly string Tag;        

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