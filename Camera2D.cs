using Microsoft.Xna.Framework;

namespace Nyvorn;

public class Camera2D
{
    public Matrix Transform { get; private set; }
    private Vector2 position;
    private int viewportWidth;
    private int viewportHeight;

    public Camera2D(int viewportWidth, int viewportHeight)
    {
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
    }

    public void Follow(Vector2 target)
    {
        position = target;

        var positionCentered = new Vector2(
            target.X - viewportWidth / 2,
            target.Y - viewportHeight / 2
        );

        Transform = Matrix.CreateTranslation(new Vector3(-positionCentered, 0));
    }
}