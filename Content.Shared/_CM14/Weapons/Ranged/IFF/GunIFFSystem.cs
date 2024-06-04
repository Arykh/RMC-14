﻿using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Weapons.Ranged.IFF;

public sealed class GunIFFSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private EntityQuery<UserIFFComponent> _userIFFQuery;

    public override void Initialize()
    {
        _userIFFQuery = GetEntityQuery<UserIFFComponent>();

        SubscribeLocalEvent<UserIFFComponent, GetIFFFactionEvent>(OnUserIFFGetFaction);
        SubscribeLocalEvent<InventoryComponent, GetIFFFactionEvent>(OnInventoryIFFGetFaction);
        SubscribeLocalEvent<HandsComponent, GetIFFFactionEvent>(OnHandsIFFGetFaction);
        SubscribeLocalEvent<ItemIFFComponent, InventoryRelayedEvent<GetIFFFactionEvent>>(OnItemIFFGetFaction);
        SubscribeLocalEvent<GunIFFComponent, AmmoShotEvent>(OnGunIFFAmmoShot);
        SubscribeLocalEvent<ProjectileIFFComponent, PreventCollideEvent>(OnProjectileIFFPreventCollide);
    }

    private void OnUserIFFGetFaction(Entity<UserIFFComponent> ent, ref GetIFFFactionEvent args)
    {
        args.Faction ??= ent.Comp.Faction;
    }

    private void OnInventoryIFFGetFaction(Entity<InventoryComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Faction != null)
            return;

        _inventory.RelayEvent(ent, ref args);
    }

    private void OnHandsIFFGetFaction(Entity<HandsComponent> ent, ref GetIFFFactionEvent args)
    {
        if (args.Faction != null)
            return;

        foreach (var (_, hand) in ent.Comp.Hands)
        {
            if (hand.HeldEntity is not { } held)
                continue;

            RaiseLocalEvent(held, ref args);
            if (args.Faction != null)
                break;
        }
    }

    private void OnItemIFFGetFaction(Entity<ItemIFFComponent> ent, ref InventoryRelayedEvent<GetIFFFactionEvent> args)
    {
        args.Args.Faction ??= ent.Comp.Faction;
    }

    private void OnGunIFFAmmoShot(Entity<GunIFFComponent> ent, ref AmmoShotEvent args)
    {
        if (!_container.TryGetContainingContainer((ent, null), out var container) ||
            !_userIFFQuery.HasComp(container.Owner))
        {
            return;
        }

        var ev = new GetIFFFactionEvent();
        RaiseLocalEvent(container.Owner, ref ev);

        if (ev.Faction is not { } id)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            var iff = EnsureComp<ProjectileIFFComponent>(projectile);
            iff.Faction = id;
            Dirty(projectile, iff);
        }
    }

    private void OnProjectileIFFPreventCollide(Entity<ProjectileIFFComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Faction is not { } faction)
        {
            return;
        }

        if (IsInFaction(args.OtherEntity, faction))
            args.Cancelled = true;
    }

    public bool IsInFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        if (!_userIFFQuery.Resolve(user, ref user.Comp, false))
            return false;

        var ev = new GetIFFFactionEvent();
        RaiseLocalEvent(user, ref ev);
        return ev.Faction == faction;
    }

    public void SetUserFaction(Entity<UserIFFComponent?> user, EntProtoId<IFFFactionComponent> faction)
    {
        user.Comp = EnsureComp<UserIFFComponent>(user);
        user.Comp.Faction = faction;
        Dirty(user);
    }
}