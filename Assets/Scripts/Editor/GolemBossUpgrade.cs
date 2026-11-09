using UnityEditor;

// Compatibility entry point for the original menu and batch command.
// The authored prefab and controllers are already upgraded.
public static class GolemBossUpgrade
{
    [MenuItem("Tools/Map 3/Upgrade Golem Boss")]
    public static void Build()
    {
        Map3GolemBuilder.Validate();
    }
}
