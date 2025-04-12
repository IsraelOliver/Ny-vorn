using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public class Animation
{
    //Variáveis
    public Texture2D Texture { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int TotalFrame { get; set; }
    public int Row { get; set; }
    public double TimePerFrame { get; set; }

    private double timer = 0;
    private int currentFrame = 0;
    private Rectangle sourceRect;

    //Contrutor da classe Animation
    public Animation(Texture2D texture, int frameWidth, int frameHeight, int totalFrame, int row, double timePerFrame)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        TotalFrame = totalFrame;
        Row = row;
        TimePerFrame = timePerFrame;

    }
    public void Update(GameTime gameTime)
    {
        timer += gameTime.ElapsedGameTime.TotalSeconds;

        if (timer >= TimePerFrame)
        {
            currentFrame++;
            if (currentFrame >= TotalFrame)
            {
                currentFrame = 0;
            }

            sourceRect = new Rectangle(currentFrame * FrameWidth, Row * FrameHeight, FrameWidth, FrameHeight);
            timer = 0;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 Position, SpriteEffects effects)
    {
        Rectangle destinationRect = new Rectangle((int)Position.X, (int)Position.Y, FrameWidth, FrameHeight);
        spriteBatch.Draw(Texture, destinationRect, sourceRect, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

}
