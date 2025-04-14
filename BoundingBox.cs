using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Nyvorn;

public class BoundingBox
{   
    protected Vector2 position;
    private int width;
    private int height;

    public BoundingBox(Vector2 position, int width, int height)
    {
        this.position = position;
        this.width = width;
        this.height = height;
    }

    public Vector2 getCenter()
    {
        return new Vector2(position.X + width / 2f, position.Y + height / 2f);
    }

    public float getTop() => position.Y ;
    public float getBottom() => position.Y + (height - 1) ;
    public float getLeft() => position.X ;
    public float getRight() => position.X + (width - 1) ;

    public int getTileLeft()    => coordinateToTile(getLeft());
    public int getTileRight()   => coordinateToTile(getRight());
    public int getTileTop()     => coordinateToTile(getTop());
    public int getTileBottom()  => coordinateToTile(getBottom());
    public int getTileCenterX() => coordinateToTile(getCenter().X);
    public int getTileCenterY() => coordinateToTile(getCenter().Y);

    private int coordinateToTile(float coordinateValue)
    {
        return (int)Math.Floor(coordinateValue / Game1.tileSize);
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        position = newPosition;
    }

    public void DrawDebug(SpriteBatch spriteBatch, Color color)
    {
        // Desenha as 4 bordas como linhas finas
        int thickness = 1;

        int left = (int)getLeft();
        int right = (int)getRight();
        int top = (int)getTop();
        int bottom = (int)getBottom();

        // Top
        spriteBatch.Draw(Game1.debugTexture, new Rectangle(left, top, width, thickness), color);
        // Bottom
        spriteBatch.Draw(Game1.debugTexture, new Rectangle(left, bottom, width, thickness), color);
        // Left
        spriteBatch.Draw(Game1.debugTexture, new Rectangle(left, top, thickness, height), color);
        // Right
        spriteBatch.Draw(Game1.debugTexture, new Rectangle(right, top, thickness, height), color);
    }
}
