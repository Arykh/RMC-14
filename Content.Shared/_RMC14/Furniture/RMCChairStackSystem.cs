using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Furniture;

public sealed class RMCChairStackSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string ContainerId = "rmc_chair_stack";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCChairStackableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCChairStackableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCChairStackableComponent, InteractHandEvent>(OnInteractHand, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<RMCChairStackableComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnMapInit(Entity<RMCChairStackableComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ContainerId);
    }

    private void OnInteractUsing(Entity<RMCChairStackableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (!TryComp<FoldableComponent>(used, out var foldable) || !foldable.IsFolded)
            return;

        if (!HasComp<RMCChairStackableComponent>(used))
            return;

        if (TryComp<WieldableComponent>(used, out var wieldable) && wieldable.Wielded)
            return;

        if (TryComp<FoldableComponent>(ent, out var entFoldable) && entFoldable.IsFolded)
            return;

        // cmss13: locate(/mob/living) in loc — check for any living mob on the tile
        if (TryComp<StrapComponent>(ent, out var strap) && strap.BuckledEntities.Count > 0)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-blocked"), ent, args.User);
            args.Handled = true;
            return;
        }

        var container = _container.EnsureContainer<Container>(ent, ContainerId);

        // Drop the item from the user's hand and insert it into the container
        if (!_hands.TryDrop(args.User, used))
            return;

        if (!_container.Insert(used, container))
        {
            _hands.TryPickupAnyHand(args.User, used);
            return;
        }

        ent.Comp.CurrentStackSize++;
        Dirty(ent);
        UpdateStackState(ent);

        if (ent.Comp.CurrentStackSize > ent.Comp.MaxStableStack)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-unstable"), ent, args.User);

            var collapseChance = Math.Sqrt(50 * ent.Comp.CurrentStackSize) / 100.0;
            if (_random.Prob((float) collapseChance))
            {
                StackCollapse(ent);
                args.Handled = true;
                return;
            }
        }

        args.Handled = true;
    }

    private void OnInteractHand(Entity<RMCChairStackableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CurrentStackSize <= 0)
            return;

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        if (container.ContainedEntities.Count == 0)
            return;

        var last = container.ContainedEntities[^1];
        if (!_container.Remove(last, container))
            return;

        _hands.TryPickupAnyHand(args.User, last);

        ent.Comp.CurrentStackSize--;
        Dirty(ent);
        UpdateStackState(ent);

        args.Handled = true;
    }

    private static void OnFoldAttempt(Entity<RMCChairStackableComponent> ent, ref FoldAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.CurrentStackSize > 0)
            args.Cancelled = true;
    }

    private void OnDestruction(Entity<RMCChairStackableComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.CurrentStackSize > 0)
            StackCollapse(ent);
    }

    private void UpdateStackState(Entity<RMCChairStackableComponent> ent)
    {
        if (ent.Comp.CurrentStackSize > 0)
        {
            var total = ent.Comp.CurrentStackSize + 1;
            _metaData.SetEntityName(ent, Loc.GetString("rmc-chair-stack-name"));
            _metaData.SetEntityDescription(ent, Loc.GetString("rmc-chair-stack-description", ("count", total)));
            _buckle.StrapSetEnabled(ent, false);
        }
        else
        {
            var meta = MetaData(ent.Owner);
            if (meta.EntityPrototype != null)
            {
                _metaData.SetEntityName(ent, meta.EntityPrototype.Name);
                _metaData.SetEntityDescription(ent, meta.EntityPrototype.Description);
            }

            _buckle.StrapSetEnabled(ent, true);
        }

        _appearance.SetData(ent.Owner, RMCChairStackVisuals.StackSize, ent.Comp.CurrentStackSize);
    }

    private void StackCollapse(Entity<RMCChairStackableComponent> ent)
    {
        _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-collapse"), ent, null);

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        var coords = Transform(ent).Coordinates;

        // Dump all stacked chairs
        var contained = new List<EntityUid>(container.ContainedEntities);
        foreach (var child in contained)
        {
            _container.Remove(child, container);
            _transform.SetCoordinates(child, coords);
            // TODO RMC14: throw chairs to random nearby turfs like cmss13
        }

        ent.Comp.CurrentStackSize = 0;
        Dirty(ent);
        UpdateStackState(ent);

        if (TryComp<FoldableComponent>(ent, out var foldable))
            _foldable.TrySetFolded(ent, foldable, true);
    }
}
