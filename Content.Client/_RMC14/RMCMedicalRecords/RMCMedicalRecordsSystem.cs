using Content.Client._RMC14.Medical.Scanner;
using Content.Shared._RMC14.RMCMedicalRecords;
using Robust.Client.Player;

namespace Content.Client._RMC14.RMCMedicalRecords;

public sealed class RMCMedicalRecordsSystem : SharedRMCMedicalRecordsSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private HealthScannerUiData? _healthScanUiData;
    private HealthScannerWindow? _storedScanWindow;

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

        _healthScanUiData = new HealthScannerUiData(EntityManager, _player);

        if (_storedScanWindow is { IsOpen: true })
            _storedScanWindow.Close();

        _storedScanWindow = new HealthScannerWindow();
        _storedScanWindow.Title = Loc.GetString("rmc-records-examine-verb-text");
        _healthScanUiData.PopulateHealthScan(_storedScanWindow, scanState);
    }
}
