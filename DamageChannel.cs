using System;
using System.Collections.Generic;

namespace Nyvorn;

public sealed class DamageChannel
{
    private readonly Queue<DamageEvent> _queue = new();

    public void Enqueue(IDamageable target, in HitInfo hit)
    {
        if (target != null && target.IsAlive)
            _queue.Enqueue(new DamageEvent(target, hit));
    }

    public void DispatchAll()
    {
        while (_queue.Count > 0)
        {
            var ev = _queue.Dequeue();
            if (ev.Target.IsAlive) ev.Target.ApplyHit(ev.Hit);
        }
    }
}