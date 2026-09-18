using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public sealed class OmaeWaMouShindeiruSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<EntityUid> _alreadyDead = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<OmaeWaMouShindeiruComponent> ent, ref MeleeHitEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            // Don't process the same target multiple times during the delay
            if (!_alreadyDead.Add(target))
                continue;

            var delay = ent.Comp.KillDelay;
            Timer.Spawn(delay,
                () =>
            {
                _alreadyDead.Remove(target);

                // Target may have been deleted during the delay.
                if (!Exists(target))
                    return;

                // Target may already be dead.
                if (TryComp<MobStateComponent>(target, out var mobState) &&
                    mobState.CurrentState == MobState.Dead)
                {
                    return;
                }

                OmaeWaMouShindeiru(target, ent.Comp.NumberOfCuts);
            });
        }
    }

    private void OmaeWaMouShindeiru(EntityUid target, int numberOfCuts)
    {
        for (var i = 0; i < numberOfCuts; i++)
        {

        }
    }
}
