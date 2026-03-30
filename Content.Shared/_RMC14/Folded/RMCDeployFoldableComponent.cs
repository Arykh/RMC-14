// ReSharper disable CheckNamespace
namespace Content.Shared.Foldable;
// ReSharper enable CheckNamespace

public sealed partial class DeployFoldableComponent
{
    /// <summary>
    /// If true, when deployed, the entity's direction will match the user's facing direction.
    /// </summary>
    [DataField]
    public bool SetDirectionOnDeploy;
}
