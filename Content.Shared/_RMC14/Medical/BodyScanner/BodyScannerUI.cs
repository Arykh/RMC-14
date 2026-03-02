using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.BodyScanner;

[Serializable, NetSerializable]
public enum BodyScannerUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum BodyScannerVisuals : byte
{
    Occupied
}

[Serializable, NetSerializable]
public enum BodyScannerVisualLayers
{
    Base
}

[Serializable, NetSerializable]
public sealed class BodyScannerEjectBuiMsg : BoundUserInterfaceMessage;
