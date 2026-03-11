using Content.Server._RMC14.RMCMedicalRecords;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCMedicalRecordsSystem _rmcMedicalRecords = default!;
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

        _rmcMedicalRecords.UpdateMedicalRecordFromScan(target);

        var state = _rmcMedicalRecords.BuildScanSnapshot(target, scanner.Comp.DetailLevel);
        _ui.SetUiState(console.Owner, HealthScannerUIKey.Key, new HealthScannerBuiState(state));
    }

    private void OnConsoleOpenChangeHolocard(Entity<BodyScannerConsoleComponent> console, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = GetEntity(args.Owner);
        var localTarget = GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
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
