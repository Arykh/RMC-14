using Content.Shared._RMC14.Storage;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Autodoc;

public abstract class SharedAutodocSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutodocComponent, ComponentInit>(OnAutodocInit);
        SubscribeLocalEvent<AutodocComponent, MapInitEvent>(OnAutodocMapInit);
        SubscribeLocalEvent<AutodocComponent, ComponentShutdown>(OnAutodocShutdown);
        SubscribeLocalEvent<AutodocComponent, EntInsertedIntoContainerMessage>(OnAutodocEntInserted);
        SubscribeLocalEvent<AutodocComponent, EntRemovedFromContainerMessage>(OnAutodocEntRemoved);
        SubscribeLocalEvent<AutodocComponent, InteractHandEvent>(OnAutodocInteractHand);

        SubscribeLocalEvent<AutodocConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<AutodocConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);

        SubscribeLocalEvent<InsideAutodocComponent, MoveInputEvent>(OnInsideAutodocMoveInput);
    }

    private void OnAutodocInit(Entity<AutodocComponent> autodoc, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(autodoc, autodoc.Comp.ContainerId);
    }

    private void OnAutodocMapInit(Entity<AutodocComponent> autodoc, ref MapInitEvent args)
    {
        // Spawn the console at offset position and link it to this autodoc
        if (_net.IsServer && autodoc.Comp.SpawnConsolePrototype != null)
        {
            var rotation = Transform(autodoc).LocalRotation;
            var rotatedOffset = rotation.RotateVec(autodoc.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(autodoc).Offset(rotatedOffset);
            var consoleId = Spawn(autodoc.Comp.SpawnConsolePrototype.Value, consoleCoords);

            // Set the console's rotation to match the autodoc + 90 degrees for sprite alignment
            _transform.SetLocalRotation(consoleId, rotation + Angle.FromDegrees(90));

            if (TryComp(consoleId, out AutodocConsoleComponent? console))
            {
                autodoc.Comp.SpawnedConsole = consoleId;
                autodoc.Comp.LinkedConsole = consoleId;
                console.LinkedAutodoc = autodoc;
                console.SpawnedByAutodoc = autodoc;
                Dirty(autodoc);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnAutodocShutdown(Entity<AutodocComponent> autodoc, ref ComponentShutdown args)
    {
        // Unlink the currently linked console (if any)
        if (autodoc.Comp.LinkedConsole is { } linkedConsoleId && TryComp(linkedConsoleId, out AutodocConsoleComponent? linkedConsole))
        {
            linkedConsole.LinkedAutodoc = null;
            Dirty(linkedConsoleId, linkedConsole);
        }

        // Only delete the console that was SPAWNED by this autodoc
        if (_net.IsServer &&
            autodoc.Comp.SpawnedConsole is { } spawnedConsoleId &&
            TryComp(spawnedConsoleId, out AutodocConsoleComponent? spawnedConsole))
        {
            if (spawnedConsole.SpawnedByAutodoc == autodoc.Owner)
            {
                QueueDel(spawnedConsoleId);
            }
        }
    }

    private void OnAutodocEntInserted(Entity<AutodocComponent> autodoc, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != autodoc.Comp.ContainerId)
            return;

        autodoc.Comp.Occupant = args.Entity;
        Dirty(autodoc);
        UpdateAutodocVisuals(autodoc);

        if (!_timing.ApplyingState)
        {
            var inside = EnsureComp<InsideAutodocComponent>(args.Entity);
            inside.Autodoc = autodoc;
            Dirty(args.Entity, inside);
        }
    }

    private void OnAutodocEntRemoved(Entity<AutodocComponent> autodoc, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != autodoc.Comp.ContainerId)
            return;

        if (autodoc.Comp.Occupant == args.Entity)
        {
            autodoc.Comp.Occupant = null;
            autodoc.Comp.IsSurgeryInProgress = false;
            autodoc.Comp.CurrentSurgeryType = AutodocSurgeryType.None;
            autodoc.Comp.HealingBrute = false;
            autodoc.Comp.HealingBurn = false;
            autodoc.Comp.HealingToxin = false;
            autodoc.Comp.BloodTransfusion = false;
            autodoc.Comp.Filtering = false;
            autodoc.Comp.RemoveLarva = false;
            autodoc.Comp.CloseIncisions = false;
            autodoc.Comp.RemoveShrapnel = false;
            Dirty(autodoc);
        }

        UpdateAutodocVisuals(autodoc);
        RemCompDeferred<InsideAutodocComponent>(args.Entity);
    }

    private void OnAutodocInteractHand(Entity<AutodocComponent> autodoc, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (autodoc.Comp.Occupant is { } occupant)
        {
            TryEjectOccupant(autodoc, occupant, args.User);
            args.Handled = true;
        }
    }

    private void OnConsoleShutdown(Entity<AutodocConsoleComponent> console, ref ComponentShutdown args)
    {
        // Clean up the autodoc's reference to this console
        if (console.Comp.LinkedAutodoc is { } linkedAutodocId && TryComp(linkedAutodocId, out AutodocComponent? linkedAutodoc))
        {
            if (linkedAutodoc.LinkedConsole == console.Owner)
            {
                linkedAutodoc.LinkedConsole = null;
                Dirty(linkedAutodocId, linkedAutodoc);
            }

            // Also clear SpawnedConsole if this console was spawned by that autodoc
            if (linkedAutodoc.SpawnedConsole == console.Owner)
            {
                linkedAutodoc.SpawnedConsole = null;
                Dirty(linkedAutodocId, linkedAutodoc);
            }
        }

        // Also check SpawnedByAutodoc in case it's different from LinkedAutodoc
        if (console.Comp.SpawnedByAutodoc is { } spawnedByAutodocId &&
            spawnedByAutodocId != console.Comp.LinkedAutodoc &&
            TryComp(spawnedByAutodocId, out AutodocComponent? spawnerAutodoc))
        {
            if (spawnerAutodoc.SpawnedConsole == console.Owner)
            {
                spawnerAutodoc.SpawnedConsole = null;
                Dirty(spawnedByAutodocId, spawnerAutodoc);
            }
        }
    }

    private void OnConsoleUIOpenAttempt(Entity<AutodocConsoleComponent> console, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (console.Comp.LinkedAutodoc is not { } autodocId || !HasComp<AutodocComponent>(autodocId))
        {
            _popup.PopupClient(Loc.GetString("rmc-autodoc-no-autodoc-connected"), console, args.User);
            args.Cancel();
        }
    }

    protected bool TryEjectOccupant(Entity<AutodocComponent> autodoc, EntityUid occupant, EntityUid? user = null)
    {
        // If surgery is in progress and user is the occupant, deny
        if (autodoc.Comp.IsSurgeryInProgress && user == occupant)
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-cannot-exit-during-surgery"), autodoc, occupant);
            return false;
        }

        // If surgery is in progress and someone else is ejecting, warn them but allow. This causes damage to the patient.
        if (autodoc.Comp.IsSurgeryInProgress && user != null && user != occupant)
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-surgery-aborted"), autodoc);
        }

        EjectOccupant(autodoc, occupant);
        return true;
    }

    private void EjectOccupant(Entity<AutodocComponent> autodoc, EntityUid occupant)
    {
        if (!_container.TryGetContainer(autodoc, autodoc.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        _audio.PlayPvs(autodoc.Comp.EjectSound, autodoc);

        if (autodoc.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(autodoc))
            _stun.TryStun(occupant, autodoc.Comp.ExitStun, true);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-ejected", ("entity", occupant)), autodoc);
    }

    private void UpdateAutodocVisuals(Entity<AutodocComponent> autodoc)
    {
        AutodocVisualState state;
        if (autodoc.Comp.Occupant == null)
            state = AutodocVisualState.Empty;
        else if (autodoc.Comp.IsSurgeryInProgress)
            state = AutodocVisualState.Operating;
        else
            state = AutodocVisualState.Occupied;

        _appearance.SetData(autodoc, AutodocVisuals.State, state);
    }

    protected void UpdateSurgeryVisuals(Entity<AutodocComponent> autodoc)
    {
        UpdateAutodocVisuals(autodoc);
    }

    private void OnInsideAutodocMoveInput(Entity<InsideAutodocComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Autodoc is not { } autodocId)
            return;

        if (!TryComp<AutodocComponent>(autodocId, out var autodoc))
            return;

        // Don't allow movement-based ejection during surgery
        if (autodoc.IsSurgeryInProgress)
            return;

        EjectOccupant((autodocId, autodoc), ent);
    }
}
