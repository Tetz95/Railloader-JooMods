using Railroader.Modding;

namespace SignalsEverywhere.Rr2026;

// Railroader 2026.1 loads SignalsEverywhere here (Info.json "assemblies"), in place of Railloader's plugin.
public sealed class ModEntryPoint : IModEntryPoint
{
    private SignalsEverywhere? _mod;

    public void OnModLoad(IModContext context)
    {
        _mod = new SignalsEverywhere(context.ModDirectory, context.CheckDependency);
        _mod.OnEnable();
    }

    public void OnModUnload()
    {
        _mod?.OnDisable();
    }
}
