using Content.Server._RMC14.RMCMedicalRecords;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMedicalRecordsSystem _rmcMedicalRecords = default!;

    protected override void OnConsoleActivateInWorld(Entity<BodyScannerConsoleComponent> console, ref ActivateInWorldEvent args)
    {
        base.OnConsoleActivateInWorld(console, ref args);

        if (!args.Handled)
            return;

        if (!TryGetLinkedScanner(console, out var scanner))
            return;

        _audio.PlayPvs(scanner.Comp.ScanSound, console);

        if (scanner.Comp.Occupant is not { } target || TerminatingOrDeleted(target))
        {
            scanner.Comp.Occupant = null;
            return;
        }

        _rmcMedicalRecords.UpdateMedicalRecordFromScan(target);
        _popup.PopupEntity(Loc.GetString("rmc-body-scanner-scan-stored", ("entity", target)), console);

        var state = _rmcMedicalRecords.BuildScanSnapshot(target, scanner.Comp.DetailLevel);
        if (_player.TryGetSessionByEntity(args.User, out var session))
            RaiseNetworkEvent(new BodyScannerConsoleScanEvent(state), session);
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
