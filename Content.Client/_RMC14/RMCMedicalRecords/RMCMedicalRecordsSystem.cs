using Content.Client._RMC14.Medical.Scanner;
using Content.Shared._RMC14.RMCMedicalRecords;
using Robust.Client.Player;

namespace Content.Client._RMC14.RMCMedicalRecords;

public sealed class RMCMedicalRecordsSystem : SharedRMCMedicalRecordsSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private HealthScannerUiData? _scanUiData;
    private HealthScannerWindow? _scanWindow;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenStoredScanEvent>(OnOpenStoredScan);
    }

    private void OnOpenStoredScan(OpenStoredScanEvent ev)
    {
        var target = GetEntity(ev.Target);
        if (!TryGetMedicalRecord(target, out var record) || record.LastScanState is not { } scanState)
            return;

        _scanUiData ??= new HealthScannerUiData(EntityManager, _player);

        if (_scanWindow is { IsOpen: true })
            _scanWindow.Close();

        _scanWindow = new HealthScannerWindow();
        _scanWindow.Title = Loc.GetString("rmc-health-analyzer-title");
        _scanUiData.PopulateHealthScan(_scanWindow, scanState);
    }
}
