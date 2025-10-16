using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    private Vector2 Position, Velocity;
    private bool facingLeft = false;
    private bool isMoving = false;
    private bool OnGround = false;

    private MouseState prevMouse, currMouse;

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
    
    private bool drawAttackHitbox = false;   // troque pra true quando quiser ver a caixa

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

        if (k.IsKeyDown(Keys.A)) { vx -= speed; isMoving = true; facingLeft = true; }
        if (k.IsKeyDown(Keys.D)) { vx += speed; isMoving = true; facingLeft = false; }

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
            
            bool canHit = false;
            int f = attackOverlay.CurrentFrame; // precisa do getter em Animation
            canHit = (f >= attackImpactStartFrame && f <= attackImpactEndFrame);

            if (canHit)
            {
                Rectangle hit = GetAttackHitbox();

                // TODO: colisão com inimigos
                // foreach (var enemy in enemies)
                //     if (hit.Intersects(enemy.Bounds)) enemy.TakeDamage(dano);
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
    
    private Rectangle GetAttackHitbox()
    {
        // AABB atual do corpo (17x23), do jeitinho que seu MoveAxis usa:contentReference[oaicite:3]{index=3}
        Rectangle body = new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            animManager.FrameWidth,   // 17
            animManager.FrameHeight   // 23
        );

        // Direção: olhando pra esquerda => bate à esquerda, caso contrário à direita
        int dir = facingLeft ? -1 : 1;

        int x = (dir > 0)
            ? body.Right                // começa na borda direita
            : body.Left - attackReachForward; // começa 11 px antes da borda esquerda

        int y = body.Y + attackYOffset;       // altura dentro do corpo

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

            Vector2 offs = attackOffsetByState[animManager.CurrentState];

            int baseW = animManager.FrameWidth;
            int overlayW = attackOverlay.FrameWidth;

            if (fx == SpriteEffects.FlipHorizontally)
            {
                float flipCompX = baseW - overlayW;
                offs.X = -offs.X + flipCompX;
            }

            spriteBatch.Draw(
                attackOverlay.Texture,
                Position + offs,
                attackOverlay.SourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                fx,
                0f
            );
        }

        if (attacking && drawAttackHitbox)
        {
            Rectangle hit = GetAttackHitbox();
        }
    }
    public Vector2 GetPosition() => Position;
}