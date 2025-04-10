using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    public Rectangle rect;
    Texture2D pixels;

    public Player(Texture2D texture)
    {
        rect = new Rectangle(100, 100, 272, 23);
        pixels = texture;
    }
    public void Update(GameTime gameTime){}
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(pixels, rect, Color.White); 
    }
}
