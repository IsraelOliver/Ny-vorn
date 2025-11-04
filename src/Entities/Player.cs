using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nyvorn.Configs; // ← novo: referência à config do player

namespace Nyvorn;

public partial class Player : IDamageable
{
    public int Health { get; private set; } = 100; // Vida do player.
    public int MaxHealth { get; private set; } = 100;
    public int DarkEnergy { get; private set; } = 10;
    public int MaxDarkEnergy { get; private set; } = 100;
    
    public Vector2 Position, Velocity;
    private bool facingLeft = false; // sensor para verificar o lado do player.
    private bool isMoving = false;   // sensor de movimento.
    private bool OnGround = false;   // sensor para ver se esta no chão.

    private MouseState prevMouse, currMouse;
    private Vector2 mouseWorld;
    public void SetMouseWorld(Vector2 world) => mouseWorld = world;

    // --- MOVIMENTO ---
    private float walkSpeed = 120f;
    private float sprintSpeed = 200f;
    private float gravity = 1800f;
    private float jumpSpeed = 500f;
    private const float Skin = 1f;

    // ===== Jump Responsivo =====
    private float CoyoteTime     = 0.12f; 
    private float JumpBufferTime = 0.12f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    private KeyboardState prevKey, currKey;

    private ManagerAnimation animManager;

    // --- ATAQUE ---
    private Animation attackOverlay; // animação do braço
    private bool attacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0.22f;

    // hitbox de ataque
    private int attackReachForward = 10;
    private int attackHeight = 12;
    private int attackYOffset = 1;

    private int attackImpactStartFrame = 2;
    private int attackImpactEndFrame = 3;

    private bool attackFacingLeft = false;
    private bool attackHitActive = false;
    private Rectangle attackHitRect;
    private bool drawAttackHitbox = false;   // true para debug

    private const float HurtStunTime   = 0.18f;
    private const float HurtKbSpeed    = 220f;
    private const float HurtKbUp       = 120f;
    private float hurtTimer = 0f;

    // --- NOVO: Config carregada via JSON ---
    private readonly PlayerConfig cfg;

    public Player(Texture2D bodyWithArm, Texture2D bodyOffHand, Texture2D attackSheet, PlayerConfig config)
    {
        cfg = config ?? new PlayerConfig();

        animManager = new ManagerAnimation(bodyWithArm, bodyOffHand);

        // --- aplica MOVIMENTO do JSON ---
        walkSpeed   = cfg.Movement.WalkSpeed;
        sprintSpeed = cfg.Movement.SprintSpeed;
        gravity     = cfg.Movement.Gravity;
        jumpSpeed   = cfg.Movement.JumpSpeed;
        CoyoteTime     = cfg.Movement.CoyoteTime;
        JumpBufferTime = cfg.Movement.JumpBuffer;
        

        // --- aplica ATAQUE do JSON ---
        attackDuration = cfg.Attack.Duration;

        if (cfg.Attack.ImpactFrames is { Length: > 0 })
        {
            attackImpactStartFrame = cfg.Attack.ImpactFrames[0];
            attackImpactEndFrame   = cfg.Attack.ImpactFrames[^1];
        }

        attackReachForward = cfg.Attack.Hitbox.ReachForward;
        attackHeight       = cfg.Attack.Hitbox.Height;
        attackYOffset      = cfg.Attack.Hitbox.YOffset;

        // overlay de ataque (animação do braço)
        int frames = 4; 
        attackOverlay = new Animation(attackSheet, 23, 23, frames, 0, 0, 0.03, loop: false);

        // vida inicial
        Health = cfg.Stats.Health;
        MaxHealth = cfg.Stats.MaxHealth;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }

        // Eneria Inicial
        DarkEnergy = cfg.Stats.DarkEnergy;
        MaxDarkEnergy = cfg.Stats.MaxDarkEnergy;
        if (DarkEnergy > MaxDarkEnergy)
        {
            DarkEnergy = MaxDarkEnergy;
        }
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        prevMouse = currMouse;
        currMouse = Mouse.GetState();
        bool leftClicked = currMouse.LeftButton == ButtonState.Pressed
                        && prevMouse.LeftButton == ButtonState.Released;

        prevKey = currKey;
        currKey = Keyboard.GetState();

        bool inHurt = hurtTimer > 0f;
        if (inHurt) hurtTimer -= dt;

        // === Movimento lateral ===
        isMoving = false;
        float speed = currKey.IsKeyDown(Keys.LeftShift) ? sprintSpeed : walkSpeed;
        float vx = 0f;

        if(!inHurt)
        {
            if (currKey.IsKeyDown(Keys.A)) { vx -= speed; isMoving = true; if (!attacking) facingLeft = true; }
            if (currKey.IsKeyDown(Keys.D)) { vx += speed; isMoving = true; if (!attacking) facingLeft = false; }
        }
        
        if(!inHurt) Velocity.X = vx;

        // === Pulo ===
        bool jumpPressedThisFrame = currKey.IsKeyDown(Keys.Space) && prevKey.IsKeyUp(Keys.Space);
        if (jumpPressedThisFrame)
            jumpBufferTimer = JumpBufferTime;

        if (OnGround) coyoteTimer = CoyoteTime;
        else          coyoteTimer -= dt;

        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;

        bool canJump = (OnGround || coyoteTimer > 0f);
        if (!inHurt && canJump && jumpBufferTimer > 0f)
        {
            Velocity.Y = -jumpSpeed;
            OnGround = false;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // === Ataque ===
        if (leftClicked && !attacking)
        {
            float playerCenterX = Position.X + animManager.FrameWidth * 0.5f;
            attackFacingLeft = mouseWorld.X < playerCenterX;
            facingLeft = attackFacingLeft;

            attacking = true;
            attackTimer = attackDuration;
            attackOverlay.Reset();                    
            animManager.UseOffHandBase(true);         
        }

        // Gravidade
        Velocity.Y += gravity * dt;

        // Colisão
        MoveAxis(Velocity.X * dt, 0f);
        MoveAxis(0f, Velocity.Y * dt);

        // Animação
        if (!OnGround) animManager.ChangeState(AnimationState.Jump);
        else animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);

        animManager.Update(gameTime);

        // Lógica do ataque
        if (attacking)
        {
            attackOverlay.Update(gameTime);           
            int f = attackOverlay.CurrentFrame;
            bool canHit = (f >= attackImpactStartFrame && f <= attackImpactEndFrame);

            if (canHit)
            {
                attackHitActive = true;
                attackHitRect   = GetAttackHitbox();
            }
            else
            {
                attackHitActive = false;
                attackHitRect   = Rectangle.Empty;
            }

            attackTimer -= dt;

            if (attackTimer <= 0f || attackOverlay.IsFinished)
            {
                attacking = false;
                animManager.UseOffHandBase(false);
                attackHitActive = false;
                attackHitRect = Rectangle.Empty;
            }
        }
    }

    public Rectangle GetBounds() =>
        new Rectangle((int)Position.X, (int)Position.Y, animManager.FrameWidth, animManager.FrameHeight);

    public bool TryGetAttackHitbox(out Rectangle rect)
    {
        rect = attackHitRect;
        return attackHitActive;
    }
    
    private Rectangle GetAttackHitbox()
    {
        Rectangle body = new Rectangle((int)Position.X, (int)Position.Y, animManager.FrameWidth, animManager.FrameHeight);

        int dir = attackFacingLeft ? -1 : 1;
        int x = (dir > 0) ? body.Right : body.Left - attackReachForward;
        int y = body.Y + attackYOffset;

        return new Rectangle(x, y, attackReachForward, attackHeight);
    }

    private void MoveAxis(float dx, float dy)
    {
        if (dx == 0 && dy == 0) return;

        Vector2 newPos = new(Position.X + dx, Position.Y + dy);
        Rectangle aabb = new(
            (int)newPos.X,
            (int)newPos.Y,
            animManager.FrameWidth,
            animManager.FrameHeight
        );

        int tileSize = Game1.tileSize;
        int minX = Math.Max(0, (aabb.Left + (int)Math.Min(0, dx) - (int)Skin) / tileSize);
        int maxX = Math.Max(0, (aabb.Right + (int)Math.Max(0, dx) + (int)Skin - 1) / tileSize);
        int minY = Math.Max(0, (aabb.Top + (int)Math.Min(0, dy) - (int)Skin) / tileSize);
        int maxY = Math.Max(0, (aabb.Bottom + (int)Math.Max(0, dy) + (int)Skin - 1) / tileSize);

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
    
    private readonly Dictionary<AnimationState, Vector2> attackOffsetByState = new()
    {
        { AnimationState.Idle,    new Vector2( 0f, 0f ) },
        { AnimationState.Walking, new Vector2( 0f, 0f ) },
        { AnimationState.Jump,    new Vector2( 0f, 0f ) },
    };

    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects fx = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        animManager.Draw(spriteBatch, Position, fx);

        if (attacking)
        {
            SpriteEffects attackFx = attackFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 offs = attackOffsetByState[animManager.CurrentState];
            int baseW = animManager.FrameWidth;
            int overlayW = attackOverlay.FrameWidth;

            if (attackFx == SpriteEffects.FlipHorizontally)
            {
                float flipCompX = baseW - overlayW;
                offs.X = -offs.X + flipCompX;
            }

            spriteBatch.Draw(
                attackOverlay.Texture,
                Position + offs,
                attackOverlay.SourceRect,
                Color.White,
                0f, Vector2.Zero, 1f,
                attackFx, 0f
            );
        }

        if (attacking && drawAttackHitbox && attackHitActive)
            spriteBatch.Draw(Game1.WhitePixel, attackHitRect, new Color(0,255,0,90));

        if (attacking && drawAttackHitbox)
        {
            Rectangle hit = GetAttackHitbox();
            spriteBatch.Draw(Game1.WhitePixel, hit, new Color(0, 255, 0, 90));
        }

        int barW = 20, barH = 4;
        var r = new Rectangle((int)Position.X - 1, (int)Position.Y - 6, barW, barH);
        float pct = Health / 100f;

        spriteBatch.Draw(Game1.WhitePixel, r, new Color(30,30,30,180));
        int w = (int)(r.Width * MathHelper.Clamp(pct, 0f, 1f));
        if (w > 0)
            spriteBatch.Draw(Game1.WhitePixel, new Rectangle(r.X, r.Y, w, r.Height), Color.LimeGreen);
    }

    public Vector2 GetPosition() => Position;
    public bool IsAlive => Health > 0;
    public Rectangle Bounds => GetBounds();
    Vector2 IDamageable.Position => Position;

    public void TakeDamage(int amount, int fromDir = 0)
        => ApplyHit(new HitInfo(amount, fromDir, HurtKbSpeed, HurtKbUp, HurtStunTime, Faction.Enemy, "Legacy"));

    public void ApplyHit(in HitInfo hit)
    {
        Health = Math.Max(0, Health - hit.Damage);
        hurtTimer = hit.Stun;

        if (hit.DirX != 0)
        {
            Velocity.X = hit.KnockbackX * Math.Sign(hit.DirX);
            Velocity.Y = -hit.KnockbackY;
        }
    }

    // Extra helper para Game1 (pegar o dano base do JSON)
    public int GetBaseDamage() => cfg.Attack.BaseDamage;
}
