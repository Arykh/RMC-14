using Content.Shared._RMC14.PowerLoader;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Furniture;

public sealed class RMCChairStackSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string ContainerId = "rmc_chair_stack";
    private const float SpeedFast = 6.67f;
    private static readonly ProtoId<ToolQualityPrototype> WrenchQuality = "Anchoring";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCChairStackableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCChairStackableComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(AnchorableSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, InteractHandEvent>(OnInteractHand, before: [typeof(SharedBuckleSystem)]);
        SubscribeLocalEvent<RMCChairStackableComponent, PowerLoaderGrabEvent>(OnPowerLoaderGrab);
        SubscribeLocalEvent<RMCChairStackableComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<RMCChairStackableComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<RMCChairStackableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCChairStackableComponent, ThrowDoHitEvent>(OnThrowDoHit);
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
        if (_tool.HasQuality(used, WrenchQuality) && ent.Comp.CurrentStackSize > 0)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-wrench-blocked"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!TryComp<FoldableComponent>(used, out var foldable) || !foldable.IsFolded)
            return;

        if (!HasComp<RMCChairStackableComponent>(used))
            return;

        if (TryComp<WieldableComponent>(used, out var wieldable) && wieldable.Wielded)
            return;

        if (TryComp<FoldableComponent>(ent, out var entFoldable) && entFoldable.IsFolded)
            return;

        if (TryComp<StrapComponent>(ent, out var strap) && strap.BuckledEntities.Count > 0)
        {
            _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-blocked"), ent, args.User);
            args.Handled = true;
            return;
        }

        var container = _container.EnsureContainer<Container>(ent, ContainerId);

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

            var collapseChance = Math.Sqrt(50 * ent.Comp.CurrentStackSize) / 100;
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

    private void OnPowerLoaderGrab(Entity<RMCChairStackableComponent> ent, ref PowerLoaderGrabEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CurrentStackSize <= 0)
        {
            var msg = Loc.GetString("rmc-chair-stack-power-loader-grab");
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            args.Handled = true;
            return;
        }

        if (ent.Comp.CurrentStackSize > ent.Comp.MaxStableStack)
        {
            var collapseChance = Math.Sqrt(50 * ent.Comp.CurrentStackSize) / 100;
            if (_random.Prob((float) collapseChance))
            {
                StackCollapse(ent);
                args.Handled = true;
            }
        }
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

    private void OnDamageChanged(Entity<RMCChairStackableComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (ent.Comp.CurrentStackSize > 0)
            StackCollapse(ent);
    }

    private void OnThrowDoHit(Entity<RMCChairStackableComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        _audio.PlayPredicted(ent.Comp.ThrownHitSound, ent, null);
    }

    private void UpdateStackState(Entity<RMCChairStackableComponent> ent)
    {
        if (ent.Comp.CurrentStackSize > 0)
        {
            var total = ent.Comp.CurrentStackSize + 1;
            _metaData.SetEntityName(ent, Loc.GetString("rmc-chair-stack-name"));
            _metaData.SetEntityDescription(ent, Loc.GetString("rmc-chair-stack-description", ("count", total)));
            _buckle.StrapSetEnabled(ent, false);
            EnsureComp<PowerLoaderGrabbableComponent>(ent);
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
            RemComp<PowerLoaderGrabbableComponent>(ent);
        }

        _appearance.SetData(ent.Owner, RMCChairStackVisuals.StackSize, ent.Comp.CurrentStackSize);
    }

    private void StackCollapse(Entity<RMCChairStackableComponent> ent)
    {
        _popup.PopupPredicted(Loc.GetString("rmc-chair-stack-collapse"), ent, null);
        _audio.PlayPredicted(ent.Comp.CollapseSound, ent, null);

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        var coords = Transform(ent).Coordinates;

        // Dump and throw the stacked chairs
        var contained = new List<EntityUid>(container.ContainedEntities);
        var remainingStack = contained.Count;
        foreach (var child in contained)
        {
            remainingStack--;
            _container.Remove(child, container);
            _transform.SetCoordinates(child, coords);

            var scatterRadius = MathF.Floor(remainingStack / 2f);
            var throwRange = _random.NextFloat(2f, 5f);
            var effectiveDistance = MathF.Max(1f, MathF.Min(scatterRadius, throwRange));
            var direction = _random.NextAngle().ToVec() * effectiveDistance;
            _throwing.TryThrow(child, direction, SpeedFast);
        }

        ent.Comp.CurrentStackSize = 0;
        Dirty(ent);
        UpdateStackState(ent);

        if (TryComp<FoldableComponent>(ent, out var foldable) && _foldable.TrySetFolded(ent, foldable, true))
        {
            var baseRange = _random.NextFloat(2f, 5f);
            var baseDirection = _random.NextAngle().ToVec() * baseRange;
            _throwing.TryThrow(ent, baseDirection, SpeedFast);
        }
    }
}
