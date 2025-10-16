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

    public Animation(Texture2D texture, int frameWidth, int frameHeight, int totalFrame, int row, int startColumn, double timePerFrame, bool loop = true)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        TotalFrame = totalFrame;
        Row = row;
        StartColumn = startColumn;
        TimePerFrame = timePerFrame;
        Loop = loop;

        sourceRect = new Rectangle(StartColumn * frameWidth, row * frameHeight, frameWidth, frameHeight);
    }
    public void Update(GameTime gameTime)
    {
        if (IsFinished) return;

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
                    currentFrame = TotalFrame - 1;
                    IsFinished = true;
                }
            }
        }

        int column = StartColumn + currentFrame;
        sourceRect = new Rectangle(column * FrameWidth, Row * FrameHeight, FrameWidth, FrameHeight);
    }

    public void Reset()
    {
        timer = 0;
        currentFrame = 0;
        IsFinished = false;
    }

    public void CopyProgressFrom(Animation other)
    {
        if (other == null) return;

        // Mapeia por fração do ciclo para suportar TotalFrame diferentes
        double frac = (other.TotalFrame > 0)
            ? (double)other.CurrentFrame / other.TotalFrame
            : 0.0;

        int mappedFrame = (int)System.Math.Round(frac * this.TotalFrame);
        mappedFrame = System.Math.Clamp(mappedFrame, 0, System.Math.Max(0, this.TotalFrame - 1));

        this.IsFinished = false;         // ao trocar folha, consideramos não-finalizada
        this.timer = 0;                  // mantém passo regular entre frames
        // se preferir preservar sensação de tempo, pode copiar uma fração de timer aqui
        this.Reset();                    // garante sourceRect coerente com frame 0
        // depois ajusta para o frame mapeado:
        for (int i = 0; i < mappedFrame; i++)
        {
            // avança o frame sem esperar tempo (salta para o frame desejado)
            // reaproveitando a lógica do Update:
            // incrementa currentFrame respeitando loop/finish
            if (i + 1 >= this.TotalFrame)
            {
                if (this.Loop) { /* volta ao início */ }
                else { this.IsFinished = true; break; }
            }
        }
        // Ajuste direto do frame (mais simples e determinístico):
        // Como currentFrame é privado, usamos um pequeno truque:
        // avançamos o sourceRect manualmente com base no mappedFrame:
        // -> substitui o loop acima por:
        typeof(Animation).GetField("currentFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, mappedFrame);
        int column = this.StartColumn + mappedFrame;
        typeof(Animation).GetField("sourceRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, new Microsoft.Xna.Framework.Rectangle(column * this.FrameWidth, this.Row * this.FrameHeight, this.FrameWidth, this.FrameHeight));
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 Position, SpriteEffects effects)
    {
        Rectangle destinationRect = new Rectangle((int)Position.X, (int)Position.Y, FrameWidth, FrameHeight);
        spriteBatch.Draw(Texture, destinationRect, sourceRect, Color.White, 0f, Vector2.Zero, effects, 0f);
        
    }

}
