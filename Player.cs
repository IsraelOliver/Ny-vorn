using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{
    public Rectangle rect;
    public Player()
    {
        rect = new Rectangle(100, 100, 272, 23);
    }
    public void Update(GameTime gameTime){}
    public void Draw()
    {
        Globals.spriteBatch.Draw(Globals.pixel, rect, Color.White); 
    }
}
