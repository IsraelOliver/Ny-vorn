using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    // Estado básico
    private Vector2 Position;
    private Vector2 Velocity;
    private bool facingLeft = true;
    private bool isMoving = false;
    private bool OnGround = false;

    // Tunáveis
    private float moveSpeed = 120f;
    private float gravity   = 1800f;
    private float jumpSpeed = 500f;
    private const float Skin = 1f; // margem anti-quinas

    private ManagerAnimation animManager;

    public Player(Texture2D spriteSheet)
    {
        animManager = new ManagerAnimation(spriteSheet);
        Position = new Vector2(100, 100);
    }

    public Vector2 GetPosition() => Position;

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Andar
        KeyboardState k = Keyboard.GetState();
        isMoving = false;
        float vx = 0f;

        if (k.IsKeyDown(Keys.A))
        {
            vx -= moveSpeed;
            isMoving = true;
            facingLeft = true;
            if (k.IsKeyDown(Keys.LeftShift)) { moveSpeed = 160f; }
        }
        if (k.IsKeyDown(Keys.D))
        {
            vx += moveSpeed;
            isMoving = true;
            facingLeft = false;
            if (k.IsKeyDown(Keys.LeftShift)) { moveSpeed = 160f; }
        }

        Velocity.X = vx;

        // Pulo
        if (k.IsKeyDown(Keys.Space) && OnGround)
        {
            Velocity.Y = -jumpSpeed;
            OnGround = false;
        }

        // Gravidade
        Velocity.Y += gravity * dt;

        // Colisão
        MoveAxis(Velocity.X * dt, 0f); // X
        MoveAxis(0f, Velocity.Y * dt); // Y

        // Animação
        if (!OnGround)
        {
            animManager.ChangeState(AnimationState.Jump);
        }
        else
        {
            animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);
        }

        animManager.Update(gameTime);
    }

    private void MoveAxis(float dx, float dy)
    {
        if (dx == 0 && dy == 0) return;

        Vector2 newPos = new Vector2(Position.X + dx, Position.Y + dy);
        Rectangle aabb = new Rectangle(
            (int)newPos.X,
            (int)newPos.Y,
            animManager.FrameWidth,
            animManager.FrameHeight
        );

        int tileSize = Game1.tileSize;
        int minX = Math.Max(0, (aabb.Left   + (int)Math.Min(0, dx) - (int)Skin) / tileSize);
        int maxX = Math.Max(0, (aabb.Right  + (int)Math.Max(0, dx) + (int)Skin - 1) / tileSize);
        int minY = Math.Max(0, (aabb.Top    + (int)Math.Min(0, dy) - (int)Skin) / tileSize);
        int maxY = Math.Max(0, (aabb.Bottom + (int)Math.Max(0, dy) + (int)Skin - 1) / tileSize);

        //OnGround = (dy > 0) ? false : OnGround; a linha que esta causando tanto bugs

        for (int ty = minY; ty <= maxY; ty++)
        {
            for (int tx = minX; tx <= maxX; tx++)
            {
                if (!Game1.IsSolid(tx, ty)) continue;

                Rectangle tileRect = new Rectangle(tx * tileSize, ty * tileSize, tileSize, tileSize);

                if (aabb.Intersects(tileRect))
                {
                    if (dx > 0) // batendo na parede direita
                    {
                        aabb.X = tileRect.Left - aabb.Width;
                        Velocity.X = 0;
                    }
                    else if (dx < 0) // parede esquerda
                    {
                        aabb.X = tileRect.Right;
                        Velocity.X = 0;
                    }
                    else if (dy > 0) // chão
                    {
                        aabb.Y = tileRect.Top - aabb.Height;
                        Velocity.Y = 0;
                        OnGround = true;
                    }
                    else if (dy < 0) // teto
                    {
                        aabb.Y = tileRect.Bottom;
                        Velocity.Y = 0;
                    }
                }
            }
        }
        Position = new Vector2(aabb.X, aabb.Y);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects fx = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        animManager.Draw(spriteBatch, Position, fx);
    }
}