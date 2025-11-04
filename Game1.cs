﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nyvorn.Configs;   // ← usa PlayerConfig
using Nyvorn.Data;      // ← usa JsonLoader

namespace Nyvorn; 

public class Game1 : Game
{
    //Variáveis
    private GraphicsDeviceManager _graphics;

    private const int VIRTUAL_W = 384;
    private const int VIRTUAL_H = 216;

    // topo da classe Game1 (perto dos outros campos)
    private enum GameMode { Playing, Paused }
    private GameMode mode = GameMode.Playing;

    private KeyboardState _prevKey, _currKey;
    private SpriteFont uiFont;               // fonte da UI (opcional, veja nota)
    private int menuIndex = 0;
    private readonly string[] menu = { "Zoom -", "Zoom +", "Reiniciar", "Sair do Jogo" };

    // ajuste de zoom por passo
    private const float ZoomStep = 0.1f;

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
    public static int tileSize = 9;
    private Map map;
    public static Map WorldMap;

    // === NOVO: manter config do player para recriações ===
    private PlayerConfig playerCfg;

    //Metodo para verificar se um tile é solido
    public static bool IsSolid(int tileX, int tileY)
    {
        // Evita erro por acesso fora da matriz
        if (WorldMap == null || !WorldMap.InBounds(tileX, tileY))
            return false;

        return WorldMap.IsSolid(tileX, tileY);
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

        uiFont = Content.Load<SpriteFont>("fonts/UiFont");

        worldRT = new RenderTarget2D(GraphicsDevice, VIRTUAL_W, VIRTUAL_H, false, 
        SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        uiRT = new RenderTarget2D(GraphicsDevice, VIRTUAL_W, VIRTUAL_H);

        screenW = GraphicsDevice.Viewport.Width;
        screenH = GraphicsDevice.Viewport.Height;
        RecomputeViewport();

        camera = new Camera2D(VIRTUAL_W, VIRTUAL_H);

        ViewW = GraphicsDevice.Viewport.Width;
        ViewH = GraphicsDevice.Viewport.Height;

        tileTexture = Content.Load<Texture2D>("tileMap/orange");

        var bodyWithArm  = Content.Load<Texture2D>("player/playerAnimation");
        var bodyOffHand  = Content.Load<Texture2D>("player/playerAnimationOffHand");
        var attackSheet  = Content.Load<Texture2D>("player/playerAttack");

        // === Lê config do player do JSON e instancia o Player com ela ===
        playerCfg = JsonLoader.LoadFromContent<PlayerConfig>("Data/player.json");
        player = new Player(bodyWithArm, bodyOffHand, attackSheet, playerCfg);
        
        enemySheet = Content.Load<Texture2D>("enemy/Enemy");
        enemies.Add(new Enemy(enemySheet, new Vector2(260, 90))); // spawn do inimigo

        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });

        // === MAPA: cria, gera e expõe no ponteiro estático ===
        int widthTiles  = 400;           // pode ajustar depois (ou vir de preset JSON futuramente)
        int heightTiles = 220;

        map = new Map(GraphicsDevice, widthTiles, heightTiles, tileSize, seed: 1337);
        map.Regenerate();                // superfície + cavernas + minério
        WorldMap = map;                  // para Game1.IsSolid(...) funcionar

        // Calcula spawn central
        Point spawnTile = map.FindCenterSpawnTile();

        // Posiciona o player acima do chão
        Vector2 spawnWorldCenter = map.TileCenterToWorld(spawnTile.X, spawnTile.Y);
        Vector2 spawnWorld = new Vector2(spawnWorldCenter.X, spawnWorldCenter.Y - map.TileSize * 0.6f);

        if (player is not null)
        {
            player.Position = spawnWorld;
        }

        // (Opcional) centraliza a câmera já no primeiro frame
        try
        {
            // camera.CenterOn(spawnWorld);
        }
        catch {}
    }

    protected override void Update(GameTime gameTime)
    {
        _prevKey = _currKey;
        _currKey = Keyboard.GetState();

        bool escDownNow   = _currKey.IsKeyDown(Keys.Escape);
        bool escWasDown   = _prevKey.IsKeyDown(Keys.Escape);
        bool escPressed   = escDownNow && !escWasDown;

        if (escPressed)
        {
            // alterna pausa
            mode = (mode == GameMode.Playing) ? GameMode.Paused : GameMode.Playing;
        }
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (mode == GameMode.Paused)
        {
            // navegação
            bool up    = _currKey.IsKeyDown(Keys.Up)    && !_prevKey.IsKeyDown(Keys.Up);
            bool down  = _currKey.IsKeyDown(Keys.Down)  && !_prevKey.IsKeyDown(Keys.Down);
            bool left  = _currKey.IsKeyDown(Keys.Left)  && !_prevKey.IsKeyDown(Keys.Left);
            bool right = _currKey.IsKeyDown(Keys.Right) && !_prevKey.IsKeyDown(Keys.Right);
            bool enter = _currKey.IsKeyDown(Keys.Enter) && !_prevKey.IsKeyDown(Keys.Enter);

            if (up)   menuIndex = (menuIndex - 1 + menu.Length) % menu.Length;
            if (down) menuIndex = (menuIndex + 1) % menu.Length;

            if (menuIndex == 0 && (left || enter))
                camera.SetZoom(camera.Zoom - ZoomStep);

            if (menuIndex == 1 && (right || enter))
                camera.SetZoom(camera.Zoom + ZoomStep);

            if (menuIndex == 2 && enter)
            {
                ResetGame();
                mode = GameMode.Playing;
            }

            if (menuIndex == 3 && enter)
            {
                Exit();
            }

            base.Update(gameTime);
            return; // não atualiza mundo enquanto pausado
        }

        // === Mouse (tela real) -> coords virtuais -> mundo (camera) ===
        var ms = Microsoft.Xna.Framework.Input.Mouse.GetState();
        int mx = ms.X, my = ms.Y;

        int sx = mx - letterboxVP.X;
        int sy = my - letterboxVP.Y;

        Vector2 mouseWorld;
        if (sx >= 0 && sy >= 0 && sx < letterboxVP.Width && sy < letterboxVP.Height && scaleInt > 0)
        {
            Vector2 virt = new Vector2(sx / (float)scaleInt, sy / (float)scaleInt);
            Matrix inv = Matrix.Invert(camera.Transform);
            mouseWorld = Vector2.Transform(virt, inv);
        }
        else
        {
            Matrix inv = Matrix.Invert(camera.Transform);
            mouseWorld = Vector2.Transform(new Vector2(mx, my), inv);
        }

        // entrega ao Player
        player.SetMouseWorld(mouseWorld);

        // === Atualiza Player ===
        player.Update(gameTime);

        // === Dano por ataque do Player nos inimigos (agora usando GetBaseDamage()) ===
        if (player.TryGetAttackHitbox(out var atk))
        {
            foreach (var e in enemies)
            {
                if (!e.IsDead && atk.Intersects(e.Bounds))
                {
                    int dir = Math.Sign(e.Position.X - player.GetPosition().X);

                    var hit = new HitInfo(
                        dmg: player.GetBaseDamage(), // ← vem do JSON via Player
                        dirX: dir,
                        kbX: 200f, kbY: 80f,
                        stun: 0.10f,
                        src: Faction.Player,
                        tag: "Slash"
                    );
                    damage.Enqueue(e, in hit);
                }
            }
        }

        // === Atualiza inimigos, limpa mortos, despacha danos ===
        foreach (var e in enemies)
            e.Update(gameTime, player, this);

        enemies.RemoveAll(e => e.IsDead);
        damage.DispatchAll();

        // === Câmera segue o player ===
        camera.FollowSmooth(player.GetPosition(), dt, 17, 23);

        // === Reset se player morreu ===
        if (player.Health <= 0)
        {
            ResetGame();
            return;
        }

        base.Update(gameTime);
    }
    
    private void ResetGame()
    {
        // replay caso player morra: reusa a mesma config já carregada
        var bodyWithArm  = Content.Load<Texture2D>("player/playerAnimation");
        var bodyOffHand  = Content.Load<Texture2D>("player/playerAnimationOffHand");
        var attackSheet  = Content.Load<Texture2D>("player/playerAttack");

        player = new Player(bodyWithArm, bodyOffHand, attackSheet, playerCfg);

        enemies.Clear();
        enemies.Add(new Enemy(enemySheet, new Vector2(260, 90)));
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(worldRT);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // 1) Mundo no RT virtual
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camera.Transform
        );

        Point viewWorld = camera.ViewportWorldSize; // já considera o Zoom
        Vector2 topLeft = camera.TopLeftWorld; 

        map.Draw(_spriteBatch, viewWorld, topLeft);

        // Player e inimigos
        player.Draw(_spriteBatch);
        foreach (var e in enemies) e.Draw(_spriteBatch);

        _spriteBatch.End();

        // 2) UI no RT próprio (se quiser HUD/menus separados)
        GraphicsDevice.SetRenderTarget(uiRT);
        GraphicsDevice.Clear(Color.Transparent);

        if (mode == GameMode.Paused)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);

            _spriteBatch.Draw(WhitePixel, new Rectangle(0, 0, VIRTUAL_W, VIRTUAL_H), new Color(0, 0, 0, 140));

            int panelW = 320, panelH = 180;
            int px = (VIRTUAL_W - panelW) / 2;
            int py = (VIRTUAL_H - panelH) / 2;

            _spriteBatch.Draw(WhitePixel, new Rectangle(px, py, panelW, panelH), new Color(20, 20, 30, 200));
            _spriteBatch.Draw(WhitePixel, new Rectangle(px, py, panelW, 2), Color.White);
            _spriteBatch.Draw(WhitePixel, new Rectangle(px, py + panelH - 2, panelW, 2), Color.White);

            if (uiFont != null)
            {
                string title = "PAUSADO";
                var titleSize = uiFont.MeasureString(title);
                _spriteBatch.DrawString(uiFont, title, new Vector2(px + (panelW - titleSize.X) / 2, py + 12), Color.White);

                int startY = py + 50;
                for (int i = 0; i < menu.Length; i++)
                {
                    string line = (i == 0) ? $"{menu[i]}  (Zoom = {camera.Zoom:0.0})" : menu[i];
                    Color c = (i == menuIndex) ? Color.Yellow : Color.White;
                    _spriteBatch.DrawString(uiFont, line, new Vector2(px + 24, startY + i * 28), c);
                    if (i == menuIndex)
                        _spriteBatch.DrawString(uiFont, ">", new Vector2(px + 8, startY + i * 28), c);
                }
                
                _spriteBatch.DrawString(uiFont, "UP/DOWN navegar  -  LEFT/RIGHT ajustar  -  Enter confirmar  -  ESC voltar",
                    new Vector2(px + 16, py + panelH - 28), new Color(220, 220, 220));
            }

            _spriteBatch.End();
        }
        else
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            // ... HUD normal aqui, se houver ...
            _spriteBatch.End();
        }

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
