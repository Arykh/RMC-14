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

namespace Content.Shared._RMC14.Medical.BodyScanner;

public abstract class SharedBodyScannerSystem : EntitySystem
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

        SubscribeLocalEvent<BodyScannerComponent, ComponentInit>(OnBodyScannerInit);
        SubscribeLocalEvent<BodyScannerComponent, MapInitEvent>(OnBodyScannerMapInit);
        SubscribeLocalEvent<BodyScannerComponent, ComponentShutdown>(OnBodyScannerShutdown);
        SubscribeLocalEvent<BodyScannerComponent, EntInsertedIntoContainerMessage>(OnBodyScannerEntInserted);
        SubscribeLocalEvent<BodyScannerComponent, EntRemovedFromContainerMessage>(OnBodyScannerEntRemoved);
        SubscribeLocalEvent<BodyScannerComponent, InteractHandEvent>(OnBodyScannerInteractHand);

        SubscribeLocalEvent<BodyScannerConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);

        SubscribeLocalEvent<InsideBodyScannerComponent, MoveInputEvent>(OnInsideBodyScannerMoveInput);
    }

    private void OnBodyScannerInit(Entity<BodyScannerComponent> scanner, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(scanner, scanner.Comp.ContainerId);
    }

    private void OnBodyScannerMapInit(Entity<BodyScannerComponent> scanner, ref MapInitEvent args)
    {
        if (_net.IsServer && scanner.Comp.SpawnConsolePrototype != null)
        {
            var rotation = Transform(scanner).LocalRotation;
            var rotatedOffset = rotation.RotateVec(scanner.Comp.ConsoleSpawnOffset);
            var consoleCoords = _transform.GetMoverCoordinates(scanner).Offset(rotatedOffset);
            var consoleId = Spawn(scanner.Comp.SpawnConsolePrototype.Value, consoleCoords);

            _transform.SetLocalRotation(consoleId, rotation);

            if (TryComp(consoleId, out BodyScannerConsoleComponent? console))
            {
                scanner.Comp.LinkedConsole = consoleId;
                console.LinkedBodyScanner = scanner;
                Dirty(scanner);
                Dirty(consoleId, console);
            }
        }
    }

    private void OnBodyScannerShutdown(Entity<BodyScannerComponent> scanner, ref ComponentShutdown args)
    {
        if (scanner.Comp.LinkedConsole is { } linkedConsoleId &&
            TryComp(linkedConsoleId, out BodyScannerConsoleComponent? linkedConsole))
        {
            var spawnedByScanner = linkedConsole.LinkedBodyScanner == scanner.Owner;
            linkedConsole.LinkedBodyScanner = null;
            Dirty(linkedConsoleId, linkedConsole);

            if (_net.IsServer && spawnedByScanner)
                QueueDel(linkedConsoleId);
        }
    }

    private void OnBodyScannerEntInserted(Entity<BodyScannerComponent> scanner, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != scanner.Comp.ContainerId)
            return;

        scanner.Comp.Occupant = args.Entity;
        Dirty(scanner);
        UpdateBodyScannerVisuals(scanner);

        if (!_timing.ApplyingState)
        {
            var inside = EnsureComp<InsideBodyScannerComponent>(args.Entity);
            inside.BodyScanner = scanner;
            Dirty(args.Entity, inside);
        }
    }

    private void OnBodyScannerEntRemoved(Entity<BodyScannerComponent> scanner, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != scanner.Comp.ContainerId)
            return;

        if (scanner.Comp.Occupant == args.Entity)
        {
            scanner.Comp.Occupant = null;
            Dirty(scanner);
        }

        UpdateBodyScannerVisuals(scanner);
        RemCompDeferred<InsideBodyScannerComponent>(args.Entity);
    }

    private void OnBodyScannerInteractHand(Entity<BodyScannerComponent> scanner, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (scanner.Comp.Occupant is { } occupant)
        {
            EjectOccupant(scanner, occupant);
            args.Handled = true;
        }
    }

    private void OnConsoleUIOpenAttempt(Entity<BodyScannerConsoleComponent> console, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (console.Comp.LinkedBodyScanner is not { } scannerId || !HasComp<BodyScannerComponent>(scannerId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-body-scanner-no-scanner-connected"), console, args.User);
            args.Cancel();
        }
    }

    protected void EjectOccupant(Entity<BodyScannerComponent> scanner, EntityUid occupant)
    {
        if (!_container.TryGetContainer(scanner, scanner.Comp.ContainerId, out var container))
            return;

        _container.Remove(occupant, container);
        _audio.PlayPvs(scanner.Comp.EjectSound, scanner);

        if (scanner.Comp.ExitStun > TimeSpan.Zero && !HasComp<NoStunOnExitComponent>(scanner))
            _stun.TryStun(occupant, scanner.Comp.ExitStun, true);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-body-scanner-ejected", ("entity", occupant)), scanner);
    }

    private void UpdateBodyScannerVisuals(Entity<BodyScannerComponent> scanner)
    {
        var occupied = scanner.Comp.Occupant != null;
        _appearance.SetData(scanner, BodyScannerVisuals.Occupied, occupied);
    }

    private void OnInsideBodyScannerMoveInput(Entity<InsideBodyScannerComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.BodyScanner is not { } scannerId)
            return;

        if (!TryComp<BodyScannerComponent>(scannerId, out var scanner))
            return;

        EjectOccupant((scannerId, scanner), ent);
    }
}
