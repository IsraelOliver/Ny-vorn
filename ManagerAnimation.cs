using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public class ManagerAnimation
{
    private Dictionary<AnimationState, Animation> playerAnimations;

    public Animation CurrentAnimation { get; private set; }

    public ManagerAnimation(Texture2D spriteSheet)
    {
        playerAnimations = new Dictionary<AnimationState, Animation>();

        playerAnimations[AnimationState.Idle] = new Animation(spriteSheet, 17, 23, 1, 1, 0);
        playerAnimations[AnimationState.Walking] = new Animation(spriteSheet, 17, 23, 15, 0, 0.04);

        CurrentAnimation = playerAnimations[AnimationState.Idle];
    }

    public void ChangeState(AnimationState newState)
    {
        if (CurrentAnimation != playerAnimations[newState])
        {
            CurrentAnimation = playerAnimations[newState];
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