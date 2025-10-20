using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public partial class Player : IDamageable
{
    public int Health { get; private set; } = 100; // Vida do player.
    
    private Vector2 Position, Velocity;
    private bool facingLeft = false; // sensor para verificar o lado do player.
    private bool isMoving = false; // sensor de movimento.
    private bool OnGround = false; // sensor para ver se esta no chão.

    private MouseState prevMouse, currMouse;
    private Vector2 mouseWorld;
    public void SetMouseWorld(Vector2 world) => mouseWorld = world;

    private float walkSpeed = 120f; // velocidade da andada
    private float sprintSpeed = 200f; // velocidade da corrida
    private float gravity = 1800f; // força da gravidade
    private float jumpSpeed = 500f; // força do pulo
    private const float Skin = 1f;

    // ===== Jump Responsivo =====
    private const float CoyoteTime     = 0.12f; 
    private const float JumpBufferTime = 0.12f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    private KeyboardState prevKey, currKey;

    private ManagerAnimation animManager;

    // ataque com overlay
    private Animation attackOverlay; //animação do braço
    private bool attacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0.22f; // velocidade do ataque

    // ataque hitbox
    private int attackReachForward = 10;  // alcance do ataque (hitbox do ataque)
    private int attackHeight = 12;        // tamanho do arco do golpe (aumenta para baixo, ajustar junto com a altura)
    private int attackYOffset = 1;        // altura do ataque (começa de cima)

    private int attackImpactStartFrame = 2;
    private int attackImpactEndFrame = 3;

    private bool attackFacingLeft = false;
 
    private bool attackHitActive = false;
    private Rectangle attackHitRect;

    private bool drawAttackHitbox = false;   // troque pra true quando quiser ver a caixa de hitbox
    
    private const float HurtStunTime   = 0.18f;
    private const float HurtKbSpeed    = 220f;
    private const float HurtKbUp       = 120f;
    private float hurtTimer = 0f;

    public Player(Texture2D bodyWithArm, Texture2D bodyOffHand, Texture2D attackSheet)
    {
        animManager = new ManagerAnimation(bodyWithArm, bodyOffHand);
        Position = new Vector2(100, 100);

        int frames = 4;
        attackOverlay = new Animation(attackSheet, 23, 23, frames, 0, 0, 0.03, loop: false);
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

        // Faz andar
        isMoving = false;
        float speed = currKey.IsKeyDown(Keys.LeftShift) ? sprintSpeed : walkSpeed;
        float vx = 0f;

        if(!inHurt)
        {
            if (currKey.IsKeyDown(Keys.A)) { vx -= speed; isMoving = true; if (!attacking) facingLeft = true; }
            if (currKey.IsKeyDown(Keys.D)) { vx += speed; isMoving = true; if (!attacking) facingLeft = false; }
        }
        
        if(!inHurt) Velocity.X = vx;

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

        // Faz atacar
        if (leftClicked && !attacking)
        {
            float playerCenterX = Position.X + animManager.FrameWidth * 0.5f;
            attackFacingLeft = mouseWorld.X < playerCenterX; // faz atacar semrpe para o mesmo lado que iniciou o ataque
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

        // Mudança de textura
        if (!OnGround) animManager.ChangeState(AnimationState.Jump);
        else animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);

        // Update das animações
        animManager.Update(gameTime);

        if (attacking)
        {
            attackOverlay.Update(gameTime);           // anima o braço de ataque

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
                attackHitRect   = Rectangle.Empty; // limpa a hitbox
            }

            attackTimer -= dt;

            if (attackTimer <= 0f || attackOverlay.IsFinished)
            {
                attacking = false;
                animManager.UseOffHandBase(false);

                attackHitActive = false; // garante que o hitbox sumiu
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
            animManager.FrameWidth,    // 17
            animManager.FrameHeight    // 23
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
            // o overlay segue a direção travada do swing
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
        {
            spriteBatch.Draw(Game1.WhitePixel, attackHitRect, new Color(0,255,0,90));
        }

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

        hurtTimer = hit.Stun; // reaproveita seu HurtStunTime se preferir

        if (hit.DirX != 0)
        {
            Velocity.X = hit.KnockbackX * Math.Sign(hit.DirX);
            Velocity.Y = -hit.KnockbackY;
        }
    }
}