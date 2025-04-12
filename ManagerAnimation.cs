using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public class ManagerAnimation
{
    private Dictionary<AnimationState, Animation> animations;

    public Animation CurrentAnimation { get; private set; }

    public ManagerAnimation(Texture2D spriteSheet)
    {
        animations = new Dictionary<AnimationState, Animation>();

        animations[AnimationState.Idle] = new Animation(spriteSheet, 17, 23, 1, 1, 0);
        animations[AnimationState.Walking] = new Animation(spriteSheet, 17, 23, 15, 0, 0.05);

        CurrentAnimation = animations[AnimationState.Idle];
    }

    public void ChangeState(AnimationState newState)
    {
        if (CurrentAnimation != animations[newState])
        {
            CurrentAnimation = animations[newState];
        }
    }

    public void Update(GameTime gameTime)
    {
        CurrentAnimation.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, SpriteEffects spriteEffect)
    {
        CurrentAnimation.Draw(spriteBatch, position, spriteEffect);
    }
}