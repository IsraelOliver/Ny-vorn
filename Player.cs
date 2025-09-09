using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn;

public class Player
{

    // Estado básico
    private Vector2 Position;
    private Vector2 Velocity;
    private bool facingLeft = false;
    private bool isMoving = false;
    private bool OnGround = false;

    public Vector2 GetPosition() => Position;

    // Tunáveis
    private float walkSpeed = 120f;
    private float sprintSpeed = 200f;
    private float gravity = 1800f;
    private float jumpSpeed = 500f;
    private const float Skin = 1f; // margem anti-quinas

    // Referências e dados do braço
    private Texture2D bodyWithArm, bodyNoArm;
    private Camera2D cam;

    // O braço está desenhado dentro do spritesheet "playerAnimation" (bodyWithArm)
    // Você disse: TOP-LEFT = (3,9), BOTTOM-RIGHT = (12,13)  → width=10, height=5
    private Rectangle armSource = new Rectangle(3, 9, 10, 5);

    // Origin (pivô) do braço DENTRO do retângulo do braço: (coluna, linha)
    // Você disse: ombro = (10,9) em coordenada absoluta da textura.
    // Convertemos para coordenada LOCAL do retângulo:
    private Vector2 armOrigin = new Vector2(10 - 3, 9 - 9); // = (7, 0)

    // Offset do OMBRO no CORPO (em pixels, relativo ao canto superior esquerdo do colisor do player)
    // Ajuste esse valor para alinhar com seu sprite (comece com algo próximo do ombro visual)
    private Point shoulderOffsetPx = new Point(8, 7);

    // Tamanho do colisor do player (para espelhar o ombro quando virar)
    private const int ColliderWidth = 17;
    private const int ColliderHeight = 23;

    // Controle do ataque
    private bool attackActive = false;
    private float attackTimer = 0f;        // 0..1
    private const float attackDuration = 0.20f; // 200ms para o swing (ajuste ao gosto)

    // Sweep/raios do arco
    private const float sweepDeg = 120f; // abertura total do golpe
    private const float sweep = MathF.PI * sweepDeg / 180f;
    private const float rx = 12f; // raio X da elipse (px)
    private const float ry = 8f;  // raio Y da elipse (px)

    // Mouse (borda de clique)
    private MouseState prevMouse, currMouse;

    private ManagerAnimation animManager;

    public Player(Texture2D bodyWithArmTex, Texture2D bodyNoArmTex, Camera2D camera)
    {
        bodyWithArm = bodyWithArmTex;
        bodyNoArm = bodyNoArmTex;
        cam = camera;

        animManager = new ManagerAnimation(bodyWithArm); // Manager usa o spritesheet padrão
        Position = new Vector2(100, 100);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Declarar
        prevMouse = currMouse;
        currMouse = Mouse.GetState();
        bool leftClicked = (currMouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released);

        Vector2 mouseWorld = cam.ScreenToWorld(new Vector2(currMouse.X, currMouse.Y));
        KeyboardState k = Keyboard.GetState();

        // Andar
        isMoving = false;
        float vx = 0f;
        float speed = (k.IsKeyDown(Keys.LeftShift) ? sprintSpeed : walkSpeed);

        if (k.IsKeyDown(Keys.A))
        {
            vx -= speed;
            isMoving = true;
            facingLeft = false;
        }
        if (k.IsKeyDown(Keys.D))
        {
            vx += speed;
            isMoving = true;
            facingLeft = true;
        }

        Velocity.X = vx;

        // Pulo
        if (k.IsKeyDown(Keys.Space) && OnGround)
        {
            Velocity.Y = -jumpSpeed;
            OnGround = false;
        }

        //Attack
        if (leftClicked && !attackActive)
        {
            attackActive = true;
            attackTimer = 0f;
            animManager.ChangeState(AnimationState.Attack);
            animManager.ResetCurrent();
        }
        if (attackActive)
        {
            attackTimer += (float)gameTime.ElapsedGameTime.TotalSeconds / attackDuration;
            if (attackTimer >= 1f)
            {
                attackTimer = 1f;
                attackActive = false;
            }
        }
        // Gravidade
        Velocity.Y += gravity * dt;

        // Colisão
        MoveAxis(Velocity.X * dt, 0f); // X
        MoveAxis(0f, Velocity.Y * dt); // Y

        // Animação

        if (animManager.IsPlaying(AnimationState.Attack) && !animManager.IsCurrentFinished)
        {
            // não troca de estado; deixa o Update da animação avançar os frames
        }
        else
        {
            // só aqui decide pulo/andar/parado
            if (!OnGround)
                animManager.ChangeState(AnimationState.Jump);
            else
                animManager.ChangeState(isMoving ? AnimationState.Walking : AnimationState.Idle);
        }

        animManager.Update(gameTime);
    }

    private void MoveAxis(float dx, float dy)
    {
        if (dx == 0 && dy == 0) return;

        Vector2 newPos = new Vector2(Position.X + dx, Position.Y + dy);
        Rectangle aabb = new Rectangle(
            (int)newPos.X,
            (int)newPos.Y,
            animManager.FrameWidth,
            animManager.FrameHeight
        );

        int tileSize = Game1.tileSize;
        int minX = Math.Max(0, (aabb.Left + (int)Math.Min(0, dx) - (int)Skin) / tileSize);
        int maxX = Math.Max(0, (aabb.Right + (int)Math.Max(0, dx) + (int)Skin - 1) / tileSize);
        int minY = Math.Max(0, (aabb.Top + (int)Math.Min(0, dy) - (int)Skin) / tileSize);
        int maxY = Math.Max(0, (aabb.Bottom + (int)Math.Max(0, dy) + (int)Skin - 1) / tileSize);

        OnGround = (dy > 1) ? false : OnGround; //agora funciona, desde que mantenha em 1.

        for (int ty = minY; ty <= maxY; ty++)
        {
            for (int tx = minX; tx <= maxX; tx++)
            {
                if (!Game1.IsSolid(tx, ty)) continue;

                Rectangle tileRect = new Rectangle(tx * tileSize, ty * tileSize, tileSize, tileSize);

                if (aabb.Intersects(tileRect))
                {
                    if (dx > 0) // batendo na parede direita
                    {
                        aabb.X = tileRect.Left - aabb.Width;
                        Velocity.X = 0;
                    }
                    else if (dx < 0) // parede esquerda
                    {
                        aabb.X = tileRect.Right;
                        Velocity.X = 0;
                    }
                    else if (dy > 0) // chão
                    {
                        aabb.Y = tileRect.Top - aabb.Height;
                        Velocity.Y = 0;
                        OnGround = true;
                    }
                    else if (dy < 0) // teto
                    {
                        aabb.Y = tileRect.Bottom;
                        Velocity.Y = 0;
                    }
                }
            }
        }
        Position = new Vector2(aabb.X, aabb.Y);
    }
    /*
    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects fx = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        animManager.Draw(spriteBatch, Position, fx);
    }
    */
    
    public void Draw(SpriteBatch spriteBatch)
    {
        SpriteEffects fxBody = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        // 1) Ombro no mundo (espelha o X quando virado)
        Vector2 shoulderWorld = Position + new Vector2(
            facingLeft ? shoulderOffsetPx.X : (ColliderWidth - shoulderOffsetPx.X),
            shoulderOffsetPx.Y
        );

        // 2) Mouse mundo (já calculado em Update; se preferir recalcule aqui)
        Vector2 mouseWorld = cam.ScreenToWorld(new Vector2(currMouse.X, currMouse.Y));

        // 3) Ângulo base até o mouse (θ)
        Vector2 dir = mouseWorld - shoulderWorld;
        float theta = MathF.Atan2(dir.Y, dir.X);

        // 4) Easing do tempo (suaviza o swing). Use uma curva simples:
        float t = attackTimer;
        float ease = t * t * (3f - 2f * t); // ease-in-out cúbico

        // 5) Ângulo do swing em torno de θ
        float phi = theta - sweep / 2f + sweep * ease;

        // 6) Nível 2: deslocamento elíptico (centro = ombro) com eixo alinhado a θ
        // offset local na elipse com parâmetro "phi"
        Vector2 local = new Vector2(
            rx * MathF.Cos(phi),
            ry * MathF.Sin(phi)
        );
        // rotaciona o offset local para o referencial alinhado a θ
        float c = MathF.Cos(theta), s = MathF.Sin(theta);
        Vector2 offset = new Vector2(
            c * local.X - s * local.Y,
            s * local.X + c * local.Y
        );
        Vector2 armPosWorld = shoulderWorld + offset;

        // 7) DESENHO — braço atrás do corpo apenas durante o ataque
        if (attackActive)
        {
            // desenhar o braço (atrás)
            spriteBatch.Draw(
                bodyWithArm,            // a arte do braço está dentro do spritesheet "com-braco"
                position: armPosWorld,
                sourceRectangle: armSource,
                color: Color.White,
                rotation: phi,          // gira em torno do origin
                origin: armOrigin,      // pivô no ombro (em coords do retângulo)
                scale: 1f,
                effects: SpriteEffects.None, // não use flip no braço — a rotação já resolve
                layerDepth: 0f          // menor que o corpo, para ficar atrás
            );
        }

        // 8) Corpo: usa textura SEM braço durante ataque; normal caso contrário
        // O ManagerAnimation usa a textura com braço, mas o Draw da Animation desenha com a TEXTURA que ela tem.
        // Para manter tudo simples, a gente troca a textura da animação atual só no draw:
        // (Se preferir, crie um overload CurrentAnimation.Draw(...) com "Texture2D override".)
        Texture2D bodyTex = attackActive ? bodyNoArm : bodyWithArm;

        // Truque simples: antes de desenhar, troque a "Texture" da animação corrente (só visual)
        var prevTex = animManager.CurrentAnimation.Texture;
        animManager.CurrentAnimation.Texture = bodyTex;

        animManager.Draw(spriteBatch, Position, fxBody); // desenha o corpo

        animManager.CurrentAnimation.Texture = prevTex; // restaura para não afetar o Update

        // OBS: se preferir, você pode criar uma sobrecarga de Draw(Texture2D override) para evitar trocar o campo.
    }
}