using System;
using Microsoft.Xna.Framework;

namespace Nyvorn;

public class Camera2D
{
    public Matrix Transform { get; private set; }
    private readonly int viewportWidth;
    private readonly int viewportHeight;

    // === ZOOM existente ===
    public float Zoom { get; private set; } = 1f;
    public float MinZoom { get; set; } = 0.5f;
    public float MaxZoom { get; set; } = 4f;

    // === NOVO: estado interno para suavização ===
    private Vector2 camPos;          // posição atual da câmera (suavizada)
    private Vector2 lastTarget;      // alvo do frame anterior (p/ look-ahead)
    private bool initialized = false;

    // Parâmetros ajustáveis:
    public float LagSeconds { get; set; } = 0.15f;   // quanto MAIOR, mais lenta a câmera
    public float LookAheadPct { get; set; } = 0.10f; // 0.00 = sem look-ahead
    public float LookAheadMax { get; set; } = 300f;  // clamp da velocidade amostrada

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
    }

    public void SetZoom(float z)
    {
        Zoom = MathHelper.Clamp(z, MinZoom, MaxZoom);
    }

    public void AddZoom(float delta) => SetZoom(Zoom + delta);

    /// <summary>
    /// Suaviza o movimento da câmera com easing exponencial + look-ahead.
    /// Chame a cada frame com dt em segundos.
    /// </summary>
    public void FollowSmooth(Vector2 target, float dt, int spriteWidth = 17, int spriteHeight = 23)
    {
        // Centraliza no meio do sprite (como no seu Follow)
        Vector2 centeredTarget = new Vector2(
            target.X + spriteWidth / 2f,
            target.Y + spriteHeight / 2f
        );

        // Metade do viewport efetivo (considerando o zoom)
        float halfW = viewportWidth / (2f * Zoom);
        float halfH = viewportHeight / (2f * Zoom);

        // Posição "ideal" do canto superior esquerdo
        Vector2 ideal = new Vector2(
            centeredTarget.X - halfW,
            centeredTarget.Y - halfH
        );

        // --- Look-ahead simples a partir do delta do alvo ---
        if (!initialized)
        {
            camPos = ideal;
            lastTarget = centeredTarget;
            initialized = true;
        }

        Vector2 vel = dt > 0 ? (centeredTarget - lastTarget) / dt : Vector2.Zero;
        float speed = vel.Length();
        if (speed > LookAheadMax)
        {
            vel *= LookAheadMax / speed; // clamp
            speed = LookAheadMax;
        }

        Vector2 look = vel * LookAheadPct; // empurra a câmera levemente na direção do movimento
        Vector2 desired = ideal + look;

        // --- Suavização exponencial (crítica) ---
        // fator = 1 - e^(-dt / Lag)
        float t = 1f - (float)Math.Exp(-dt / Math.Max(0.0001f, LagSeconds));
        camPos = Vector2.Lerp(camPos, desired, t);

        // Snap para pixel inteiro (evita jitter de subpixel)
        float camX = (float)Math.Floor(camPos.X);
        float camY = (float)Math.Floor(camPos.Y);

        Transform = Matrix.CreateTranslation(new Vector3(-camX, -camY, 0))
                  * Matrix.CreateScale(Zoom);

        lastTarget = centeredTarget;
    }

    public Vector2 TopLeftWorld => new Vector2(
        (float)Math.Floor(camPos.X),
        (float)Math.Floor(camPos.Y)
    );

    public Point ViewportWorldSize =>
        new Point(
            (int)Math.Ceiling(viewportWidth / Zoom),
            (int)Math.Ceiling(viewportHeight / Zoom)
        );

    

    // (o seu Follow antigo pode continuar aqui, intacto)
    /*     public void Follow(Vector2 target, int spriteWidth = 17, int spriteHeight = 23)
    {
        // Centraliza no meio do sprite
        Vector2 centeredTarget = new Vector2(
            target.X + spriteWidth / 2f,
            target.Y + spriteHeight / 2f
        );

        // Metade do viewport efetivo levando em conta o zoom
        float halfW = viewportWidth  / (2f * Zoom);
        float halfH = viewportHeight / (2f * Zoom);

        // snap pra pixel inteiro pra evitar tremedeira
        float camX = (float)Math.Floor(centeredTarget.X - halfW);
        float camY = (float)Math.Floor(centeredTarget.Y - halfH);

        // aplica o zoom na matriz de view
        Transform = Matrix.CreateTranslation(new Vector3(-camX, -camY, 0))
                  * Matrix.CreateScale(Zoom);
    } */
}