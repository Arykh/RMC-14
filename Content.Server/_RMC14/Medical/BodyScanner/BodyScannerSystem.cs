using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Temperature;
using Content.Shared.FixedPoint;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyScannerConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<BodyScannerConsoleComponent, OpenChangeHolocardUIEvent>(OnConsoleOpenChangeHolocard);
        SubscribeLocalEvent<BodyScannerComponent, EntRemovedFromContainerMessage>(OnOccupantRemoved);
    }

    private void OnConsoleUIOpened(Entity<BodyScannerConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        if (!TryGetLinkedScanner(console, out var scanner))
            return;

        _audio.PlayPvs(scanner.Comp.ScanSound, console);
        UpdateUI(console, scanner);
    }

    private void OnConsoleOpenChangeHolocard(Entity<BodyScannerConsoleComponent> console, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = GetEntity(args.Owner);
        var localTarget = GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }

    private void OnOccupantRemoved(Entity<BodyScannerComponent> scanner, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != scanner.Comp.ContainerId)
            return;

        if (scanner.Comp.LinkedConsole is not { } consoleId || !TryComp(consoleId, out BodyScannerConsoleComponent? consoleComp))
            return;

        if (!_ui.IsUiOpen(consoleId, HealthScannerUIKey.Key))
            return;

        SendUISnapshot((consoleId, consoleComp));
    }

    private void SendUISnapshot(Entity<BodyScannerConsoleComponent> console)
    {
        if (console.Comp.LastScanSnapshot is { } snapshot)
        {
            _ui.SetUiState(console.Owner, HealthScannerUIKey.Key, snapshot);
        }
        else
        {
            _ui.SetUiState(console.Owner,
                HealthScannerUIKey.Key,
                new HealthScannerBuiState(NetEntity.Invalid, 0, 0, null, string.Empty, null, false));
        }
    }

    private void UpdateUI(Entity<BodyScannerConsoleComponent> console, Entity<BodyScannerComponent> scanner)
    {
        if (scanner.Comp.Occupant is not { } target || TerminatingOrDeleted(target))
            return;

        FixedPoint2 blood = 0;
        FixedPoint2 maxBlood = 0;
        if (_rmcBloodstream.TryGetBloodSolution(target, out var bloodstream))
        {
            blood = bloodstream.Volume;
            maxBlood = bloodstream.MaxVolume;
        }

        _rmcBloodstream.TryGetChemicalSolution(target, out _, out var chemicals);
        _rmcTemperature.TryGetCurrentTemperature(target, out var temperature);

        var pulse = _rmcPulse.TryGetPulseReading(target, true, out _);
        var bleeding = _rmcBloodstream.IsBleeding(target);

        var state = new HealthScannerBuiState(GetNetEntity(target),
            blood,
            maxBlood,
            temperature,
            pulse,
            chemicals,
            bleeding,
            scanner.Comp.DetailLevel);

        console.Comp.LastScanSnapshot = state;
        _ui.SetUiState(console.Owner, HealthScannerUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var consoles = EntityQueryEnumerator<BodyScannerConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, HealthScannerUIKey.Key))
                continue;

            if (!TryGetLinkedScanner((uid, console), out var scanner) || scanner.Comp.Occupant == null)
                continue;

            if (time < console.UpdateAt)
                continue;

            console.UpdateAt = time + console.UpdateCooldown;
            UpdateUI((uid, console), scanner);
        }
    }

    private bool TryGetLinkedScanner(Entity<BodyScannerConsoleComponent> console, out Entity<BodyScannerComponent> scanner)
    {
        scanner = default;
        if (console.Comp.LinkedBodyScanner is not { } linkedId || !TryComp(linkedId, out BodyScannerComponent? comp))
            return false;

        scanner = (linkedId, comp);
        return true;
    }
}
