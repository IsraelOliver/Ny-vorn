using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    private Vector2 Position;
    private Animation animation;
    private float playerSpeed = 100f;
    private bool facingLeft = true;

    public Player(Texture2D spriteSheet)
    {
        animation = new Animation(spriteSheet, 17, 23, 16, 0.05);
        Position = new Vector2(100, 100); // Irá mudar futuramente quando adicionar um mapa
    }

    public void Move(GameTime gameTime)
    {
        KeyboardState kState = Keyboard.GetState();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float updateSpeed = playerSpeed * deltaTime;

        if (kState.IsKeyDown(Keys.W))
        {
            Position.Y -= (int)updateSpeed; 
        }
        if (kState.IsKeyDown(Keys.S))
        {
            Position.Y += (int)updateSpeed;
        }
        if (kState.IsKeyDown(Keys.A))
        {
            Position.X -= (int)updateSpeed;
            facingLeft = true;
        }
        if (kState.IsKeyDown(Keys.D))
        {
            Position.X += (int)updateSpeed;
            facingLeft = false;
        }

    }

    public void Update(GameTime gameTime)
    {
        Move(gameTime);
        animation.Update(gameTime);
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects spriteEffect = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        animation.Draw(spriteBatch, Position, spriteEffect); 
    }
}
