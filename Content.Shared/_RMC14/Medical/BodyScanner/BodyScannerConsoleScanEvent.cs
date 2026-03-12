using Content.Shared._RMC14.Medical.Scanner;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.BodyScanner;

/// <summary>
///     Sent from the server to a specific client when the body scanner console is activated,
///     carrying the scan snapshot so the client can open an unbound health-scan window.
/// </summary>
[Serializable, NetSerializable]
public sealed class BodyScannerConsoleScanEvent(HealthScanState scanState) : EntityEventArgs
{
    public readonly HealthScanState ScanState = scanState;
}
