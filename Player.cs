using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    public Rectangle rect;
    float playerSpeed = 100f;

    Texture2D pixels;

    public Player(Texture2D texture)
    {
        rect = new Rectangle(100, 100, 272, 23);
        pixels = texture;
    }

    public void Move(GameTime gameTime)
    {
        KeyboardState kState = Keyboard.GetState();
        float updateSpeed = playerSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kState.IsKeyDown(Keys.W))
        {
            rect.Y -= (int)updateSpeed; 
        }
        if (kState.IsKeyDown(Keys.S))
        {
            rect.Y += (int)updateSpeed;
        }
        if (kState.IsKeyDown(Keys.A))
        {
            rect.X -= (int)updateSpeed;
        }
        if (kState.IsKeyDown(Keys.D))
        {
            rect.X += (int)updateSpeed;
        }

    }
    
    public void Update(GameTime gameTime)
    {
        Move(gameTime);
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(pixels, rect, Color.White); 
    }
}
