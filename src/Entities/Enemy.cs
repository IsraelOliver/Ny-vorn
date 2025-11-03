using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public partial class Enemy : IDamageable
{
    public Vector2 Position;
    private Vector2 Velocity;
    private bool OnGround;
    private bool facingLeft = true;

    public int Health { get; private set; } = 100;
    public bool IsDead => Health <= 0;

    Vector2 IDamageable.Position => Position;

    private const float Gravity = 1800f;
    private const float WalkSpeed = 60f;
    private const float ChaseSpeed = 90f;
    private const float PatrolDuration = 2.2f;   
    private const float AggroDistance = 120f;      // distancia para perseguir
    private const float JumpSpeed = 420f;
    private const int   StepAhead = 2;      // sensor de parede

    // contato / i-frames
    private const float ContactCooldown = 0.45f;   // tempo para dar dano no player
    private float contactTimer = 0f;

    private const float HurtIframes = 0.25f;       // evita múltiplos hits
    private float hurtTimer = 0f;

    private const float KnockbackTime     = 0.3f;
    private const float KnockbackFriction = 5000f;
    private float knockbackTimer = 0f;

    private readonly Texture2D sheet;
    private readonly Animation idle;
    private readonly Animation walk;
    private Animation current;

    private enum AIState { Idle, Wander, Chase }
    private AIState state = AIState.Idle;

    private static readonly System.Random rng = new System.Random();
    private float stateTimer = 0f;      
    private int wanderDir = 1;          

    private readonly Vector2 idleDurRange   = new Vector2(0.7f, 2.0f);
    private readonly Vector2 walkDurRange   = new Vector2(1.2f, 3.0f);

    public Enemy(Texture2D spriteSheet, Vector2 spawn)
    {
        sheet = spriteSheet;

        walk = new Animation(sheet, frameWidth: 15, frameHeight: 22,
                             totalFrame: 7, row: 0, startColumn: 0,
                             timePerFrame: 0.08, loop: true);

        idle = new Animation(sheet, 15, 22, 7, 1, 0, 0.15, loop: true);

        current = idle;
        Position = spawn;
    }

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, current.FrameWidth, current.FrameHeight);

    public void TakeDamage(int amount, int knockbackDir = 0)
    {
        if (IsDead || hurtTimer > 0f) return;
        Health -= amount;
        hurtTimer = HurtIframes;

        if (knockbackDir != 0)
        {
            Velocity.X = 200f * -MathF.Sign(knockbackDir);
            Velocity.Y = -80f;
            knockbackTimer = KnockbackTime;
        }
    }

    private float RandRange(Vector2 r) => (float)(r.X + rng.NextDouble() * (r.Y - r.X));

    private void PickIdle()
    {
        state = AIState.Idle;
        stateTimer = RandRange(idleDurRange);
        Velocity.X = 0f;
        current = idle;
    }

    private void PickWander()
    {
        state = AIState.Wander;
        stateTimer = RandRange(walkDurRange);
        wanderDir = rng.Next(0, 2) == 0 ? -1 : 1; 
        current = walk;
    }

    public void Update(GameTime gt, Player player, Game1 game)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        if (IsDead) return;

        if (contactTimer > 0f) contactTimer -= dt;
        if (hurtTimer > 0f) hurtTimer -= dt;

        Vector2 toPlayer = player.GetPosition() - Position;
        float dist = toPlayer.Length();

        float targetVX = 0f;

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= dt;

            float sign = MathF.Sign(Velocity.X);
            float decel = KnockbackFriction * dt;
            if (MathF.Abs(Velocity.X) <= decel) Velocity.X = 0f;
            else Velocity.X -= decel * sign;

            targetVX = Velocity.X;
        }
        else
        {
            if (dist <= AggroDistance)
            {
                state = AIState.Chase;
                int dir = Math.Sign(toPlayer.X);
                targetVX = dir * ChaseSpeed;
                current = walk;
            }
            else
            {
                if (state == AIState.Chase) { PickIdle(); }

                stateTimer -= dt;
                switch (state)
                {
                    case AIState.Idle:
                        targetVX = 0f;
                        current = idle;
                        if (stateTimer <= 0f)
                        {
                            if (rng.NextDouble() < 0.6) PickWander(); else PickIdle();
                        }
                        break;

                    case AIState.Wander:
                        targetVX = wanderDir * WalkSpeed;
                        current = walk;

                        int dir = Math.Sign(targetVX);
                        if (OnGround && dir != 0 && WallAhead(dir))
                        {
                            if (rng.NextDouble() < 0.5)
                            {
                                wanderDir *= -1;
                                targetVX = wanderDir * WalkSpeed;
                            }
                            else
                            {
                                Velocity.Y = -JumpSpeed * 0.9f;
                                OnGround = false;
                            }
                        }

                        if (stateTimer <= 0f)
                        {
                            if (rng.NextDouble() < 0.5) PickIdle();
                            else { wanderDir = -wanderDir; PickWander(); }
                        }
                        break;
                }
            }
        }

        Velocity.X = targetVX;

        if (Math.Abs(Velocity.X) > 1f)
        facingLeft = Velocity.X > 0;

        int dirX = Math.Sign(Velocity.X);
        if (OnGround && dirX != 0 && WallAhead(dirX))
        {
            Velocity.Y = -JumpSpeed;
            OnGround = false;
        }

        Velocity.Y += Gravity * dt;

        MoveAxis(Velocity.X * dt, 0f);
        MoveAxis(0f, Velocity.Y * dt);

        current.Update(gt);

        // dano no player
        if (contactTimer <= 0f && this.Bounds.Intersects(player.GetBounds()))
        {
            int dir = Math.Sign((player.GetPosition() - Position).X);
            var hit = new HitInfo(25, dir, kbX: 220f, kbY: 120f, stun: 0.18f, src: Faction.Enemy, tag: "Contact");

            game.DamageBus.Enqueue(player, in hit);
            contactTimer = ContactCooldown;
        }
    }
    
    private bool WallAhead(int dir)
    {
        int tileSize = Game1.tileSize;
        int footY = (int)(Position.Y + current.FrameHeight - 1);
        int frontX = (int)(Position.X + (dir > 0 ? current.FrameWidth + StepAhead : -StepAhead));

        int tx = Math.Max(0, frontX / tileSize);
        int ty = Math.Max(0, footY  / tileSize);

        return Game1.IsSolid(tx, ty);
    }

    private void MoveAxis(float dx, float dy)
    {
        if (dx == 0 && dy == 0) return;

        Vector2 newPos = new(Position.X + dx, Position.Y + dy);
        Rectangle aabb = new((int)newPos.X, (int)newPos.Y, current.FrameWidth, current.FrameHeight);

        int tileSize = Game1.tileSize;
        int minX = Math.Max(0, (aabb.Left + (int)Math.Min(0, dx) - 1) / tileSize);
        int maxX = Math.Max(0, (aabb.Right + (int)Math.Max(0, dx) + 1 - 1) / tileSize);
        int minY = Math.Max(0, (aabb.Top + (int)Math.Min(0, dy) - 1) / tileSize);
        int maxY = Math.Max(0, (aabb.Bottom + (int)Math.Max(0, dy) + 1 - 1) / tileSize);

        OnGround = (dy > 1) ? false : OnGround;

        for (int ty = minY; ty <= maxY; ty++)
            for (int tx = minX; tx <= maxX; tx++)
            {
                if (!Game1.IsSolid(tx, ty)) continue;

                Rectangle tileRect = new(tx * tileSize, ty * tileSize, tileSize, tileSize);

                if (aabb.Intersects(tileRect))
                {
                    if (dx > 0) { aabb.X = tileRect.Left - aabb.Width; Velocity.X = 0; }
                    else if (dx < 0) { aabb.X = tileRect.Right; Velocity.X = 0; }
                    else if (dy > 0) { aabb.Y = tileRect.Top - aabb.Height; Velocity.Y = 0; OnGround = true; }
                    else if (dy < 0) { aabb.Y = tileRect.Bottom; Velocity.Y = 0; }
                }
            }
        Position = new Vector2(aabb.X, aabb.Y);
    }
    
    private static Texture2D _white;
    private static void Game1_UnsafeDrawBar(SpriteBatch sb, Rectangle rect, float pct)
    {
        if (_white == null) _white = Game1.WhitePixel;
        // fundo
        sb.Draw(_white, rect, new Color(50, 10, 10, 180));
        // fill
        int w = (int)(rect.Width * MathHelper.Clamp(pct, 0f, 1f));
        if (w > 0) sb.Draw(_white, new Rectangle(rect.X, rect.Y, w, rect.Height), new Color(180, 30, 30));
    }

    public void Draw(SpriteBatch sb)
    {
        var fx = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        current.Draw(sb, Position, fx);

        if (!IsDead)
        {
            int barW = 18, barH = 4;
            var r = new Rectangle((int)Position.X - 1, (int)Position.Y - 6, barW, barH);
            float pct = Health / 100f;
            // usa a função do Game1
            Game1_UnsafeDrawBar(sb, r, pct);
        }
    }
    
    public bool IsAlive => !IsDead;

    public void ApplyHit(in HitInfo hit)
    {
        if (IsDead || hurtTimer > 0f) return;

        Health -= hit.Damage;
        hurtTimer = HurtIframes;

        // knockback
        if (hit.DirX != 0)
        {
            Velocity.X = hit.KnockbackX * -MathF.Sign(hit.DirX);
            Velocity.Y = -hit.KnockbackY;
            knockbackTimer = KnockbackTime;
        }
    }
}