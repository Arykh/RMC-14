using Content.Server._RMC14.RMCRecords;
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

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCRecordsSystem _records = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyScannerConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<BodyScannerConsoleComponent, OpenChangeHolocardUIEvent>(OnConsoleOpenChangeHolocard);
    }

    private void OnConsoleUIOpened(Entity<BodyScannerConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        if (!TryGetLinkedScanner(console, out var scanner))
            return;

        _audio.PlayPvs(scanner.Comp.ScanSound, console);

        if (scanner.Comp.Occupant is not { } target)
            return;

        if (TerminatingOrDeleted(target))
        {
            scanner.Comp.Occupant = null;
            return;
        }

        // Update the patient's medical record with a snapshot of their current state
        _records.UpdateMedicalRecordFromScan(target);

        SendScanState(console, scanner, target);
    }

    private void OnConsoleOpenChangeHolocard(Entity<BodyScannerConsoleComponent> console, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = GetEntity(args.Owner);
        var localTarget = GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }

    private void SendScanState(Entity<BodyScannerConsoleComponent> console, Entity<BodyScannerComponent> scanner, EntityUid target)
    {
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

        var state = new HealthScannerBuiState(
            GetNetEntity(target),
            blood,
            maxBlood,
            temperature,
            pulse,
            chemicals,
            bleeding,
            scanner.Comp.DetailLevel);

        _ui.SetUiState(console.Owner, HealthScannerUIKey.Key, state);
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
