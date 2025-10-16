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
    public int StartColumn { get; set; }
    public bool Loop { get; set; } = true;
    public bool IsFinished { get; set; } = false;
    public int Row { get; set; }
    public double TimePerFrame { get; set; }

    private double timer = 0;
    private int currentFrame = 0;
    private Rectangle sourceRect;

    public int CurrentFrame => currentFrame;
    public Rectangle SourceRect => sourceRect;

    //Contrutor da classe Animation
    public Animation(Texture2D texture, int frameWidth, int frameHeight, int totalFrame, int row, int startColumn, double timePerFrame, bool loop = true)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        TotalFrame = totalFrame;
        Row = row;
        StartColumn = startColumn;   // <<< usa offset
        TimePerFrame = timePerFrame;
        Loop = loop;

        sourceRect = new Rectangle(StartColumn * frameWidth, row * frameHeight, frameWidth, frameHeight);
    }
    public void Update(GameTime gameTime)
    {
        if (IsFinished) return; // se não loopa e já acabou

        timer += gameTime.ElapsedGameTime.TotalSeconds;

        if (timer >= TimePerFrame)
        {
            timer = 0;
            currentFrame++;

            if (currentFrame >= TotalFrame)
            {
                if (Loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = TotalFrame - 1; // trava no último frame
                    IsFinished = true;
                }
            }
        }

        int column = StartColumn + currentFrame; // <<< aplica offset
        sourceRect = new Rectangle(column * FrameWidth, Row * FrameHeight, FrameWidth, FrameHeight);
    }

    public void Reset()
    {
        timer = 0;
        currentFrame = 0;
        IsFinished = false;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 Position, SpriteEffects effects)
    {
        Rectangle destinationRect = new Rectangle((int)Position.X, (int)Position.Y, FrameWidth, FrameHeight);
        spriteBatch.Draw(Texture, destinationRect, sourceRect, Color.White, 0f, Vector2.Zero, effects, 0f);
    }

}
