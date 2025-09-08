using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public class ManagerAnimation
{
    private Dictionary<AnimationState, Animation> playerAnimations;

    public AnimationState CurrentState { get; private set; } = AnimationState.Idle;  // estado corrente
    public bool IsCurrentFinished => CurrentAnimation.IsFinished;                     // terminou?
    public void ResetCurrent() => CurrentAnimation.Reset();                           // recomeça do 1º frame
    public bool IsPlaying(AnimationState s) => CurrentState == s;                     // helper

    public Animation CurrentAnimation { get; private set; }
    public int FrameWidth => CurrentAnimation.FrameWidth;
    public int FrameHeight => CurrentAnimation.FrameHeight;

    public ManagerAnimation(Texture2D spriteSheet)
    {
        playerAnimations = new Dictionary<AnimationState, Animation>();

        playerAnimations[AnimationState.Idle] = new Animation(spriteSheet, 23, 23, 15, 1, 0, 0.15);
        playerAnimations[AnimationState.Jump] = new Animation(spriteSheet, 23, 23, 1, 2, 0, 0);
        playerAnimations[AnimationState.Walking] = new Animation(spriteSheet, 23, 23, 15, 0, 0, 0.03);
        playerAnimations[AnimationState.Attack] = new Animation(spriteSheet, 23, 23, 6, 2, 1, 0.02, false);

        CurrentAnimation = playerAnimations[AnimationState.Idle];
    }

    //Trocar o estado da animação
    public void ChangeState(AnimationState newState)
    {
        if (CurrentAnimation != playerAnimations[newState])
        {
            CurrentAnimation = playerAnimations[newState];
            CurrentState = newState; // <-- registra o enum atual
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