using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nyvorn; 

public class Game1 : Game
{
    //Variáveis
    private GraphicsDeviceManager _graphics;

    private const int VIRTUAL_W = 384;
    private const int VIRTUAL_H = 216;

    private RenderTarget2D worldRT;
    private RenderTarget2D uiRT;

    private int screenW, screenH;
    private int scaleInt = 1;
    private Viewport letterboxVP;

    private SpriteBatch _spriteBatch;
    private Texture2D tileTexture;
    private Player player;
    private Camera2D camera;
    public static Texture2D WhitePixel;

    public static int ViewW, ViewH;

    public DamageChannel damage = new DamageChannel();
    public DamageChannel DamageBus => damage;

    private Texture2D enemySheet;
    private System.Collections.Generic.List<Enemy> enemies = new();

    // MAPA //
    public static int tileSize = 23;

    //Mapa feito com matriz
    private static int[,] map = new int[,] {
	{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
	{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1 },
	{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
    };

    //Metodo para verificar se um tile é solido

    public static bool IsSolid(int tileX, int tileY)
    {
        // Evita erro por acesso fora da matriz
        if (tileY < 0 || tileY >= map.GetLength(0) || tileX < 0 || tileX >= map.GetLength(1))
            return false;

        return map[tileY, tileX] == 1;
    }

    //Contrutor da classe Game1
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }
    
    private void RecomputeViewport() {
        scaleInt = Math.Max(1, Math.Min(screenW / VIRTUAL_W, screenH / VIRTUAL_H));
        int vpW = VIRTUAL_W * scaleInt;
        int vpH = VIRTUAL_H * scaleInt;
        int vpX = (screenW - vpW) / 2;
        int vpY = (screenH - vpH) / 2;
        letterboxVP = new Viewport(vpX, vpY, vpW, vpH);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        worldRT = new RenderTarget2D(GraphicsDevice, VIRTUAL_W, VIRTUAL_H, false, 
        SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        uiRT = new RenderTarget2D(GraphicsDevice, VIRTUAL_W, VIRTUAL_H);

        screenW = GraphicsDevice.Viewport.Width;
        screenH = GraphicsDevice.Viewport.Height;
        RecomputeViewport(); // função que você cria (abaixo)

        camera = new Camera2D(VIRTUAL_W, VIRTUAL_H);

        ViewW = GraphicsDevice.Viewport.Width;
        ViewH = GraphicsDevice.Viewport.Height;

        tileTexture = Content.Load<Texture2D>("tileMap/orange");

        var bodyWithArm  = Content.Load<Texture2D>("player/playerAnimation");
        var bodyOffHand  = Content.Load<Texture2D>("player/playerAnimationOffHand");
        var attackSheet  = Content.Load<Texture2D>("player/playerAttack");

        player = new Player(bodyWithArm, bodyOffHand, attackSheet);
        
        enemySheet = Content.Load<Texture2D>("enemy/Enemy");
        enemies.Add(new Enemy(enemySheet, new Vector2(260, 90))); // spawn do inimigo

        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });
 
        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        // Sair com ESC/back (igual ao teu)
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // === Mouse (tela real) -> coords virtuais -> mundo (camera) ===
        var ms = Microsoft.Xna.Framework.Input.Mouse.GetState();
        int mx = ms.X, my = ms.Y;

        // Remove offset do letterbox/pillarbox
        int sx = mx - letterboxVP.X;
        int sy = my - letterboxVP.Y;

        Vector2 mouseWorld;
        if (sx >= 0 && sy >= 0 && sx < letterboxVP.Width && sy < letterboxVP.Height && scaleInt > 0)
        {
            // volta para a resolução virtual pixel-perfect
            Vector2 virt = new Vector2(sx / (float)scaleInt, sy / (float)scaleInt);

            // remove a transformação da câmera
            Matrix inv = Matrix.Invert(camera.Transform);
            mouseWorld = Vector2.Transform(virt, inv);
        }
        else
        {
            // fallback: se o mouse estiver fora do viewport, só inverte a câmera da posição bruta
            Matrix inv = Matrix.Invert(camera.Transform);
            mouseWorld = Vector2.Transform(new Vector2(mx, my), inv);
        }

        // entrega ao Player (como você já faz hoje)
        player.SetMouseWorld(mouseWorld);  // :contentReference[oaicite:2]{index=2}

        // === Atualiza Player ===
        player.Update(gameTime);           // :contentReference[oaicite:3]{index=3}

        // === Dano por ataque do Player nos inimigos (igual ao teu) ===
        if (player.TryGetAttackHitbox(out var atk))   // :contentReference[oaicite:4]{index=4}
        {
            foreach (var e in enemies)
            {
                if (!e.IsDead && atk.Intersects(e.Bounds))
                {
                    int dir = Math.Sign(e.Position.X - player.GetPosition().X);

                    var hit = new HitInfo(
                        dmg: 20, dirX: dir,
                        kbX: 200f, kbY: 80f,
                        stun: 0.10f,
                        src: Faction.Player,
                        tag: "Slash"
                    );
                    damage.Enqueue(e, in hit);       // :contentReference[oaicite:5]{index=5}
                }
            }
        }

        // === Atualiza inimigos, limpa mortos, despacha danos (igual ao teu) ===
        foreach (var e in enemies)
            e.Update(gameTime, player, this);        // :contentReference[oaicite:6]{index=6}

        enemies.RemoveAll(e => e.IsDead);            // :contentReference[oaicite:7]{index=7}
        damage.DispatchAll();                        // :contentReference[oaicite:8]{index=8}

        // === Câmera segue o player (igual ao teu) ===
        camera.FollowSmooth(player.GetPosition(), dt, 17, 23);         // :contentReference[oaicite:9]{index=9}

        // === Reset se player morreu (igual ao teu) ===
        if (player.Health <= 0)                      // :contentReference[oaicite:10]{index=10}
        {
            ResetGame();
            return;
        }

        base.Update(gameTime);
    }
    
    private void ResetGame()
    {
        // replay caso player morra
        var bodyWithArm  = Content.Load<Texture2D>("player/playerAnimation");
        var bodyOffHand  = Content.Load<Texture2D>("player/playerAnimationOffHand");
        var attackSheet  = Content.Load<Texture2D>("player/playerAttack");

        player = new Player(bodyWithArm, bodyOffHand, attackSheet);

        enemies.Clear();
        enemies.Add(new Enemy(enemySheet, new Vector2(260, 90)));
    }

    protected override void Draw(GameTime gameTime)
    {

        // TODO: Add your drawing code here
        GraphicsDevice.SetRenderTarget(worldRT);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // 1) Mundo no RT virtual
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.Transform
        );
            // tiles, player, inimigos já existem:
            // (o seu loop do mapa + player.Draw + inimigos.Draw)  :contentReference[oaicite:2]{index=2}
            for (int y = 0; y < map.GetLength(0); y++)
                for (int x = 0; x < map.GetLength(1); x++)
                    if (map[y, x] == 1)
                        _spriteBatch.Draw(tileTexture, new Vector2(x * tileSize, y * tileSize), Color.White);

            player.Draw(_spriteBatch);                // usa internamente ManagerAnimation/Attack etc. 
            foreach (var e in enemies) e.Draw(_spriteBatch);  // :contentReference[oaicite:4]{index=4}
        _spriteBatch.End();

        // 2) UI no RT próprio (se quiser HUD/menus separados)
        GraphicsDevice.SetRenderTarget(uiRT);
        GraphicsDevice.Clear(Color.Transparent);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // desenhe HUD aqui (barras, texto etc.)
        _spriteBatch.End();

        // 3) Compose no backbuffer com letterbox/pillarbox
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.Viewport = letterboxVP;

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied
        );
        _spriteBatch.Draw(worldRT, destinationRectangle: new Rectangle(0, 0, VIRTUAL_W * scaleInt, VIRTUAL_H * scaleInt), Color.White);
        _spriteBatch.Draw(uiRT,    destinationRectangle: new Rectangle(0, 0, VIRTUAL_W * scaleInt, VIRTUAL_H * scaleInt), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
