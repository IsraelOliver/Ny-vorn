using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    public int Health { get; private set; } = 100;
    
    private Vector2 Position, Velocity;
    private bool facingLeft = false;
    private bool isMoving = false;
    private bool OnGround = false;

    private MouseState prevMouse, currMouse;
    private Vector2 mouseWorld;
    public void SetMouseWorld(Vector2 world) => mouseWorld = world;

    private float walkSpeed = 120f; // velocidade da andada
    private float sprintSpeed = 200f; //velocidade da corrida
    private float gravity = 1800f; // força da gravidade
    private float jumpSpeed = 500f; // força do pulo
    private const float Skin = 1f;

    private ManagerAnimation animManager;

    // ataque com overlay
    private Animation attackOverlay; //animação do braço
    private bool attacking = false;
    private float attackTimer = 0f;
    private float attackDuration = 0.22f; // velocidade do ataque

    // ataque hitbox
    private int attackReachForward = 11;  // alcance pra frente (px) — você mediu 11
    private int attackHeight = 10;        // “espessura” vertical do golpe (ajuste fino)
    private int attackYOffset = 6;        // deslocamento vertical dentro do corpo (0 = topo)

    private int attackImpactStartFrame = 2;
    private int attackImpactEndFrame = 3;

    private bool attackFacingLeft = false;
 
    private bool attackHitActive = false;
    private Rectangle attackHitRect;
    
    private bool drawAttackHitbox = true;   // troque pra true quando quiser ver a caixa

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

        var k = Keyboard.GetState();

        // ANDAR
        isMoving = false;
        float speed = k.IsKeyDown(Keys.LeftShift) ? sprintSpeed : walkSpeed;
        float vx = 0f;

        if (k.IsKeyDown(Keys.A)) { vx -= speed; isMoving = true; if (!attacking) facingLeft = true; }
        if (k.IsKeyDown(Keys.D)) { vx += speed; isMoving = true; if (!attacking) facingLeft = false; }

        Velocity.X = vx;

        // PULO
        if (k.IsKeyDown(Keys.Space) && OnGround)
        {
            Velocity.Y = -jumpSpeed;
            OnGround = false;
        }

        // ATAQUE (inicia)
        if (leftClicked && !attacking)
        {
            float playerCenterX = Position.X + animManager.FrameWidth * 0.5f;
            attackFacingLeft = mouseWorld.X < playerCenterX; // trava direção do swing

            attacking = true;
            attackTimer = attackDuration;
            attackOverlay.Reset();                    // começa no frame 0:contentReference[oaicite:6]{index=6}
            animManager.UseOffHandBase(true);         // troca base para SEM braço de trás
        }

        // Gravidade
        Velocity.Y += gravity * dt;

        // Colisão (usa FrameWidth/Height da base ativa, 17x23):contentReference[oaicite:7]{index=7}
        MoveAxis(Velocity.X * dt, 0f);
        MoveAxis(0f, Velocity.Y * dt);

        // ESTADOS do corpo (não troca pra Attack)
        if (!OnGround) animManager.ChangeState(AnimationState.Jump);
        else animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);

        // Update das animações
        animManager.Update(gameTime);

        if (attacking)
        {
            attackOverlay.Update(gameTime);           // anima o braço:contentReference[oaicite:8]{index=8}

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
                attackHitRect   = Rectangle.Empty; // <-- limpa
            }

            attackTimer -= dt;

            // terminou por tempo OU a overlay chegou no fim (sem loop)
            if (attackTimer <= 0f || attackOverlay.IsFinished)
            {
                attacking = false;
                animManager.UseOffHandBase(false);    // volta base COM braço
            }
        }
    }

    public Rectangle GetBounds() =>
    new Rectangle((int)Position.X, (int)Position.Y, animManager.FrameWidth, animManager.FrameHeight);

    public void TakeDamage(int amount, int fromDir = 0)
    {
        Health = Math.Max(0, Health - amount);
        if (fromDir != 0)
            Velocity.X = 140f * -Math.Sign(fromDir);
    }
    
    public bool TryGetAttackHitbox(out Rectangle rect)
    {
        rect = attackHitRect;
        return attackHitActive;
    }
    
    private Rectangle GetAttackHitbox()
    {
        Rectangle body = new Rectangle((int)Position.X, (int)Position.Y, animManager.FrameWidth, animManager.FrameHeight);

        // use a direção travada do swing
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

        OnGround = (dy > 1) ? false : OnGround; // teu comentário original:contentReference[oaicite:9]{index=9}

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

        // no Draw, quando attacking:
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

        // fundo
        spriteBatch.Draw(Game1.WhitePixel, r, new Color(30,30,30,180));
        // preenchimento
        int w = (int)(r.Width * MathHelper.Clamp(pct, 0f, 1f));
        if (w > 0)
            spriteBatch.Draw(Game1.WhitePixel, new Rectangle(r.X, r.Y, w, r.Height), Color.LimeGreen);
    }
    public Vector2 GetPosition() => Position;
}