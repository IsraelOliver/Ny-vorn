using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{   
    //Variáveis
    private Vector2 Position;
    private ManagerAnimation animManager;
    private float playerSpeed = 100f;
    private bool facingLeft = true;
    private bool isMoving = false;

    //Contrutor da classe PLayer
    public Player(Texture2D spriteSheet)
    {
        animManager = new ManagerAnimation(spriteSheet);
        Position = new Vector2(100, 100); // Irá mudar futuramente quando adicionar um mapa e gravidade
        int width = animManager.FrameWidth;
        int height = animManager.FrameHeight;
    }

    //Método para movimentação
    public void Move(GameTime gameTime)
    {
        KeyboardState kState = Keyboard.GetState(); // Detecta o estado do teclado
        isMoving = false; // Define o movimento como parado(false)

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float updateSpeed = playerSpeed * deltaTime; //Atualiza a velocidade.

        if (kState.IsKeyDown(Keys.A))
        {
            Position.X -= (int)updateSpeed;
            isMoving = true;
            facingLeft = true;
        }

        if (kState.IsKeyDown(Keys.D))
        {
            Position.X += (int)updateSpeed;
            isMoving = true;
            facingLeft = false;
        }

        if (kState.IsKeyDown(Keys.W))
        {
            Position.Y -= (int)updateSpeed;
            isMoving = true;
        }

        if (kState.IsKeyDown(Keys.S))
        {
            Position.Y += (int)updateSpeed;
            isMoving = true;
        }

        //Especifica qual animação usar dependendo do que o player está fazendo.
        animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);

    }

    public Vector2 GetPosition()
    {
        return Position;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            animManager.FrameWidth,
            animManager.FrameHeight
        );
    }

    public void Update(GameTime gameTime)
    {
        Move(gameTime);
        animManager.Update(gameTime);
    }
    public void Draw(SpriteBatch spriteBatch)
    {   
        //Efeito para virar para o lado certo.
        SpriteEffects spriteEffect = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        animManager.Draw(spriteBatch, Position, spriteEffect); 
    }
}
