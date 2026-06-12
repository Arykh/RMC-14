using Content.Shared._RMC14.Movement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CryoCell;

public abstract class SharedCryoCellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMovementSystem _rmcMovement = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnCryoCellInit);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoCellEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoCellEntRemoved);
        SubscribeLocalEvent<CryoCellComponent, InteractHandEvent>(OnCryoCellInteractHand);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoCellMoveInput);
    }

    private void OnCryoCellInit(Entity<CryoCellComponent> cell, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(cell, cell.Comp.ContainerId);
    }

    private void OnCryoCellEntInserted(Entity<CryoCellComponent> cell, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.ContainerId)
            return;

        cell.Comp.Occupant = args.Entity;

        Dirty(cell);
        UpdateCryoCellVisuals(cell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cell.Comp.ContainerId)
            return;

        if (cell.Comp.Occupant == args.Entity)
        {
            cell.Comp.Occupant = null;
            Dirty(cell);
        }

        UpdateCryoCellVisuals(cell);
        RemCompDeferred<InsideCryoCellComponent>(args.Entity);
        _rmcMovement.SuppressCollisionOnExit(args.Entity, cell.Owner);
    }

    private void OnCryoCellInteractHand(Entity<CryoCellComponent> cell, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (cell.Comp.Occupant is { } occupant)
        {
            EjectOccupant(cell, occupant);
            args.Handled = true;
        }
    }

    private void OnInsideCryoCellMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cellId)
            return;

        if (!TryComp<CryoCellComponent>(cellId, out var cellComp))
            return;

        EjectOccupant((cellId, cellComp), ent);
    }

    private void EjectOccupant(Entity<CryoCellComponent> cell, EntityUid occupant)
    {
        if (!_container.TryGetContainer(cell, cell.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);

        if (_net.IsClient)
            return;

        _audio.PlayPvs(cell.Comp.HealingCompleteSound, cell);
        _popup.PopupEntity(Loc.GetString("rmc-body-scanner-ejected", ("entity", occupant)), cell);
    }

    private void UpdateCryoCellVisuals(Entity<CryoCellComponent> cell)
    {
        var occupied = cell.Comp.Occupant != null;
        _appearance.SetData(cell, CryoCellVisuals.Occupied, occupied);
    }
}
