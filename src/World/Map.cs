using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Requer FastNoiseLite.cs no projeto.
public enum Tile : byte
{
    Air   = 0,
    Ground= 1,
    Cave  = 2, // opcional: marca "vazio" cavado; visualmente tratamos como Air
    Ore   = 3
}

public sealed class Map
{
    // Tamanho em tiles
    public int Width  { get; }
    public int Height { get; }

    // Tamanho de cada tile em pixels (ex.: 11)
    public int TileSize { get; }

    // Buffer linear: index = x + y*Width
    public Tile[] Tiles { get; private set; }

    // Ruídos
    private readonly FastNoiseLite heightNoise;
    private readonly FastNoiseLite caveNoise;
    private readonly FastNoiseLite oreNoise;

    // Parâmetros de geração
    public int Seed { get; private set; }

    /// <summary>Escala (quanto maior, mais suave) da superfície.</summary>
    public float SurfaceScale { get; set; } = 120f;

    /// <summary>Escala das cavernas (granulosidade).</summary>
    public float CaveScale { get; set; } = 22f;

    /// <summary>Escala dos minérios (tendência a clusters).</summary>
    public float OreScale { get; set; } = 18f;

    /// <summary>Linha média do terreno em tiles (a partir do topo).</summary>
    public int Baseline { get; set; }

    /// <summary>Amplitude vertical das colinas/vales em tiles.</summary>
    public int Amplitude { get; set; }

    /// <summary>Limiar para abrir cavernas (0..1) — maior = menos cavernas.</summary>
    public float CaveThreshold { get; set; } = 0.55f;

    /// <summary>Corte para espalhar minério (0..1) — menor = menos minério.</summary>
    public float OreCutoff { get; set; } = 0.10f;

    // Desenho
    private Texture2D _pixel; // 1x1 branco
    private bool _colorsInitialized;
    private Color _groundColor = new Color(90, 66, 48);
    private Color _caveColor   = new Color(25, 25, 35);
    private Color _oreColor    = new Color(180, 180, 210);

    public Map(GraphicsDevice gd, int widthTiles, int heightTiles, int tileSize, int seed)
    {
        if (widthTiles <= 0 || heightTiles <= 0) throw new ArgumentException("Dimensions must be > 0.");
        if (tileSize   <= 0) throw new ArgumentException("TileSize must be > 0.");

        Width = widthTiles;
        Height = heightTiles;
        TileSize = tileSize;

        Tiles = new Tile[Width * Height];

        // Defaults baseados na altura total
        Baseline = Height / 3;
        Amplitude = Math.Max(4, Height / 4);

        Seed = seed;
        heightNoise = new FastNoiseLite(seed);
        caveNoise   = new FastNoiseLite(seed * 31 + 7);
        oreNoise    = new FastNoiseLite(seed * 73 + 3);

        ConfigureNoises();

        // 1x1 pixel para debug draw
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _colorsInitialized = true;
    }

    private void ConfigureNoises()
    {
        // Superfície
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        heightNoise.SetFrequency(1f / Math.Max(1f, SurfaceScale));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        heightNoise.SetFractalOctaves(4);

        // Cavernas
        caveNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        caveNoise.SetFrequency(1f / Math.Max(1f, CaveScale));

        // Minério
        oreNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        oreNoise.SetFrequency(1f / Math.Max(1f, OreScale));
    }

    /// <summary>Regenera o mundo com uma nova seed (ou a atual), usando os parâmetros correntes.</summary>
    public void Regenerate(int? newSeed = null)
    {
        if (newSeed.HasValue)
        {
            Seed = newSeed.Value;
            heightNoise.SetSeed(Seed);
            caveNoise.SetSeed(Seed * 31 + 7);
            oreNoise.SetSeed(Seed * 73 + 3);
        }

        ConfigureNoises();
        Array.Fill(Tiles, Tile.Air);
        GenerateSurface(Baseline, Amplitude);
        CarveCaves(CaveThreshold);
        PlaceOres(OreCutoff);
    }

    #region Procedural Generation

    /// <summary>Gera a superfície: abaixo da altura vira Ground.</summary>
    public void GenerateSurface(int baseline, int amplitude)
    {
        Baseline = baseline;
        Amplitude = amplitude;

        for (int x = 0; x < Width; x++)
        {
            float n = heightNoise.GetNoise(x, 0);           // [-1,1]
            float u = (n * 0.5f + 0.5f);                     // [0,1]
            int h = baseline + (int)(amplitude * u);
            h = Math.Clamp(h, 0, Height - 1);

            for (int y = h; y < Height; y++)
            {
                Tiles[Index(x, y)] = Tile.Ground;
            }
        }
    }

    /// <summary>Esculpe cavernas dentro do Ground, promovendo a Air/Cave.</summary>
    public void CarveCaves(float threshold = 0.55f)
    {
        CaveThreshold = threshold;

        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            int i = Index(x, y);
            if (Tiles[i] == Tile.Ground)
            {
                float n = caveNoise.GetNoise(x, y) * 0.5f + 0.5f; // [0,1]
                if (n > threshold)
                {
                    // Pode marcar como Air direto; Cave é útil pra diferenciar visualmente.
                    Tiles[i] = Tile.Cave; // ou Tile.Air;
                }
            }
        }
    }

    /// <summary>Espalha minério em áreas de Ground (antes de cavar ou após, seu design).</summary>
    public void PlaceOres(float cutoff = 0.10f)
    {
        OreCutoff = cutoff;

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = Index(x, y);
                if (Tiles[i] == Tile.Ground)
                {
                    float n = oreNoise.GetNoise(x, y) * 0.5f + 0.5f; // [0,1]
                    if (n < cutoff)
                        Tiles[i] = Tile.Ore;
                }
            }
    }
    
    public Point FindCenterSpawnTile()
    {
        int tx = Width / 2;
        for (int ty = 0; ty < Height; ty++)
        {
            if (IsSolid(tx, ty))
            {
                // retorna o tile logo ACIMA do chão (onde o player deve estar)
                int standY = Math.Max(0, ty - 1);
                return new Point(tx, standY);
            }
        }
        // fallback se algo deu muito errado
        return new Point(tx, 0);
    }

    // Converte (tx,ty) pro centro do tile em coordenadas de MUNDO (pixels)
    public Vector2 TileCenterToWorld(int tx, int ty)
    {
        return new Vector2(
            tx * TileSize + TileSize * 0.5f,
            ty * TileSize + TileSize * 0.5f
        );
    }

    #endregion

    #region Editing & Queries

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public int Index(int x, int y) => x + y * Width;

    public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    public Tile GetTile(int x, int y) => InBounds(x, y) ? Tiles[Index(x, y)] : Tile.Air;

    public void SetTile(int x, int y, Tile tile)
    {
        if (InBounds(x, y)) Tiles[Index(x, y)] = tile;
    }

    public void FillRect(int x, int y, int w, int h, Tile tile)
    {
        int maxX = Math.Min(Width, x + w);
        int maxY = Math.Min(Height, y + h);
        for (int yy = Math.Max(0, y); yy < maxY; yy++)
        for (int xx = Math.Max(0, x); xx < maxX; xx++)
            Tiles[Index(xx, yy)] = tile;
    }

    /// <summary>Pincel circular simples em tiles (não subpixel).</summary>
    public void BrushCircle(int centerX, int centerY, int radius, Tile tile)
    {
        int r2 = radius * radius;
        int minX = Math.Max(0, centerX - radius);
        int maxX = Math.Min(Width - 1, centerX + radius);
        int minY = Math.Max(0, centerY - radius);
        int maxY = Math.Min(Height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            int dx = x - centerX;
            int dy = y - centerY;
            if (dx * dx + dy * dy <= r2)
                Tiles[Index(x, y)] = tile;
        }
    }

    /// <summary>Retorna true se o tile é sólido para colisão.</summary>
    public bool IsSolid(int tileX, int tileY)
    {
        if (!InBounds(tileX, tileY)) return false;
        var t = Tiles[Index(tileX, tileY)];
        return t == Tile.Ground || t == Tile.Ore; // Cave/Air = não sólido
    }

    public static Point WorldToTile(Vector2 world, int tileSize)
    {
        return new Point(
            (int)MathF.Floor(world.X / tileSize),
            (int)MathF.Floor(world.Y / tileSize)
        );
    }

    public Rectangle TileBounds(int tx, int ty)
    {
        return new Rectangle(tx * TileSize, ty * TileSize, TileSize, TileSize);
    }

    #endregion

    #region Drawing (debug/simple)

    /// <summary>
    /// Desenha os tiles visíveis com cores sólidas (debug). Para performance, use culling.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch já iniciado (Begin) com transform da câmera.</param>
    /// <param name="viewportPx">Tamanho do viewport em pixels.</param>
    /// <param name="cameraPosPx">Posição da câmera em pixels (canto superior esquerdo).</param>
    public void Draw(SpriteBatch spriteBatch, Point viewportPx, Vector2 cameraPosPx)
    {
        if (!_colorsInitialized || _pixel?.GraphicsDevice?.IsDisposed == true)
            return;

        // Culling básico
        int minX = Math.Max(0, (int)MathF.Floor(cameraPosPx.X / TileSize) - 1);
        int minY = Math.Max(0, (int)MathF.Floor(cameraPosPx.Y / TileSize) - 1);
        int maxX = Math.Min(Width  - 1, (int)MathF.Ceiling((cameraPosPx.X + viewportPx.X) / TileSize) + 1);
        int maxY = Math.Min(Height - 1, (int)MathF.Ceiling((cameraPosPx.Y + viewportPx.Y) / TileSize) + 1);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            Tile t = Tiles[Index(x, y)];
            if (t == Tile.Air) continue; // skip

            Color c = t switch
            {
                Tile.Ground => _groundColor,
                Tile.Cave   => _caveColor,   // visualiza cavernas removidas (se desejar ocultar, trate como Air)
                Tile.Ore    => _oreColor,
                _           => Color.Magenta
            };

            Rectangle r = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
            spriteBatch.Draw(_pixel, r, c);
        }
    }

    #endregion
}
