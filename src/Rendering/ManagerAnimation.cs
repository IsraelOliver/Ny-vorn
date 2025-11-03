using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nyvorn;

public class ManagerAnimation
{
    private Dictionary<AnimationState, Animation> bodyWithArm;
    private Dictionary<AnimationState, Animation> bodyOffHand;

    private Dictionary<AnimationState, Animation> activeSet;

    public AnimationState CurrentState { get; private set; } = AnimationState.Idle;
    public Animation CurrentAnimation { get; private set; }
    public bool IsCurrentFinished => CurrentAnimation.IsFinished;
    public int FrameWidth => CurrentAnimation.FrameWidth;
    public int FrameHeight => CurrentAnimation.FrameHeight;

    public ManagerAnimation(Texture2D sheetWithArm, Texture2D sheetOffHand)
    {
        bodyWithArm = new()
        {
            [AnimationState.Idle]    = new Animation(sheetWithArm, 17, 23, 15, 1, 0, 0.15),
            [AnimationState.Walking] = new Animation(sheetWithArm, 17, 23, 15, 0, 0, 0.03),
            [AnimationState.Jump]    = new Animation(sheetWithArm, 17, 23, 1,  2, 0, 0.00)
        };

        bodyOffHand = new()
        {
            [AnimationState.Idle]    = new Animation(sheetOffHand, 17, 23, 15, 1, 0, 0.15),
            [AnimationState.Walking] = new Animation(sheetOffHand, 17, 23, 15, 0, 0, 0.03),
            [AnimationState.Jump]    = new Animation(sheetOffHand, 17, 23, 1,  2, 0, 0.00)
        };

        activeSet = bodyWithArm;
        CurrentAnimation = activeSet[AnimationState.Idle];
    }

    public void UseOffHandBase(bool useOffHand)
    {
        var newSet = useOffHand ? bodyOffHand : bodyWithArm;
        if (ReferenceEquals(activeSet, newSet)) return;

        var oldAnim = CurrentAnimation;
        var oldState = CurrentState;

        activeSet = newSet;

        var newAnim = activeSet[oldState];
        if (!ReferenceEquals(newAnim, oldAnim))
        {
            newAnim.CopyProgressFrom(oldAnim);
            CurrentAnimation = newAnim;
        }
    }

    public void ChangeState(AnimationState s)
    {
        if (CurrentAnimation != activeSet[s])
        {
            CurrentAnimation = activeSet[s];
            CurrentState = s;
        }
    }

    public void ResetCurrent() => CurrentAnimation.Reset();

    public bool IsPlaying(AnimationState s) => CurrentState == s;

    public void Update(GameTime gt) => CurrentAnimation.Update(gt);

    public void Draw(SpriteBatch sb, Vector2 pos, SpriteEffects fx)
        => CurrentAnimation.Draw(sb, pos, fx);
}