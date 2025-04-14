using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{   
    //Variáveis
    private Vector2 Position;
    private ManagerAnimation animManager;
    private BoundingBox boundingBox;
    private float playerSpeed = 100f;
    private bool facingLeft = true;
    private bool isMoving = false;

    float gravity = 200f;
    float velocityY = 0f;
    float maxFallSpeed = 1000f;

    //Contrutor da classe PLayer
    public Player(Texture2D spriteSheet)
    {
        animManager = new ManagerAnimation(spriteSheet);
        Position = new Vector2(100, 100); // Irá mudar futuramente quando adicionar um mapa e gravidade
        int width = animManager.FrameWidth;
        int height = animManager.FrameHeight;

        boundingBox = new BoundingBox(Position, width, height);
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

        //Especifica qual animação usar dependendo do que o player está fazendo.
        animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);

    }

    //Metodo para aplicar a gravidade ao player || FUTURAMENTE TERÁ SUA PROPRIA CLASSE
    private void ApplyGravity(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Aplica a aceleração da gravidade
        velocityY += gravity * deltaTime;

        // Limita a velocidade de queda
        if (velocityY > maxFallSpeed)
            velocityY = maxFallSpeed;

        // Move o player no eixo Y
        Position.Y += velocityY * deltaTime;

        // Atualiza o bounding box antes de verificar colisões
        boundingBox.UpdatePosition(Position);

        // Verifica colisão com o chão
        int tileBottom = boundingBox.getTileBottom();
        int tileX = boundingBox.getTileCenterX();

        if (Game1.IsSolid(tileX, tileBottom))
        {
            // Corrige a posição do jogador exatamente em cima do tile
            Position.Y = tileBottom * Game1.tileSize - animManager.FrameHeight;

            // Reseta a velocidade vertical, já que está no chão
            velocityY = 0f;
        }

        handleCollisions();
    }

    private void handleCollisions()
    {
        int tileBottom = boundingBox.getTileBottom();
        int tileX = boundingBox.getTileCenterX();

        //bottom
        if (Game1.IsSolid(tileX, tileBottom))
        {
            // Reposiciona o jogador exatamente em cima do tile
            Position.Y = tileBottom * Game1.tileSize - animManager.FrameHeight;
        }
    }

    public void Update(GameTime gameTime)
    {
        Move(gameTime);
        ApplyGravity(gameTime); 
        boundingBox.UpdatePosition(Position); // Atualiza o bounding box com a nova posição
        animManager.Update(gameTime);
    }
    public void Draw(SpriteBatch spriteBatch)
    {   
        //Efeito para virar para direita
        SpriteEffects spriteEffect = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        animManager.Draw(spriteBatch, Position, spriteEffect); 
    }

    public void DrawDebug(SpriteBatch spriteBatch)
    {
        boundingBox.DrawDebug(spriteBatch, Color.Red);
    }
}
