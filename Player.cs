using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{   
    //Variáveis
    private Vector2 Position;
    private Animation walkAnimation;
    private Animation idleAnimation;
    private Animation currentAnimation;
    private float playerSpeed = 100f;
    private bool facingLeft = true;
    private bool isMoving = false;

    //Contrutor da classe PLayer
    public Player(Texture2D spriteSheet)
    {
        walkAnimation = new Animation(spriteSheet, 17, 23, 15, 0, 0.05);
        idleAnimation = new Animation(spriteSheet, 17, 23, 1, 1, 0);
        currentAnimation = idleAnimation;
        Position = new Vector2(100, 100); // Irá mudar futuramente quando adicionar um mapa e gravidade
    }

    //Método para movimentação
    public void Move(GameTime gameTime)
    {
        KeyboardState kState = Keyboard.GetState(); // Detecta o estado do teclado
        isMoving = false;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float updateSpeed = playerSpeed * deltaTime; //Atualiza a velocidade.

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

        currentAnimation = isMoving ? walkAnimation : idleAnimation;

    }

    public void Update(GameTime gameTime)
    {
        Move(gameTime);
        currentAnimation.Update(gameTime);
    }
    public void Draw(SpriteBatch spriteBatch)
    {   
        //Efeito para virar para direita
        SpriteEffects spriteEffect = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        currentAnimation.Draw(spriteBatch, Position, spriteEffect); 
    }
}
