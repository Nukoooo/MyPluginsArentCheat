using System;
using System.Linq;
using System.Reflection;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ExposedObject;
using Newtonsoft.Json.Linq;

namespace MyPluginsArentCheat;

// Credit goes to NightmareXiv

public class EntryPoint : IDalamudPlugin
{
    private readonly object _pluginManager;
    private readonly Type _stateEnum;

    public EntryPoint([RequiredVersion("1.0")] DalamudPluginInterface pi)
    {
        _pluginManager = Exposed.From(pi.GetType()
                                        .Assembly.GetType("Dalamud.Service`1", true)
                                        .MakeGenericType(pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)))
                                .Get();
        _stateEnum = pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.Types.PluginState");
        
        RemoveBannedPlugins();
        UnbanInstalledPlugins();
    }

    public string Name => "MyPluginsArentCheat";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void RemoveBannedPlugins()
    {
        var bannedPlugins = _pluginManager.GetType().GetField("bannedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var arrayType = bannedPlugins.FieldType.GetElementType();
        var emptyArray = Array.CreateInstance(arrayType, 0);
        bannedPlugins.SetValue(_pluginManager, emptyArray);
    }

    private void UnbanInstalledPlugins()
    {
        var installedPlugins = Exposed.From(_pluginManager).InstalledPlugins;
        foreach (var plugin in installedPlugins)
        {
            var localPlugin = (object)plugin;
            var state = localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin)?.ToString();
            if (state == "LoadError")
            {
                localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.SetValue(localPlugin, _stateEnum.GetEnumValues().GetValue(0));
                var manifest = localPlugin.GetType().GetProperty("Manifest", BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin);
                manifest!.GetType().GetProperty("Disabled", BindingFlags.Public | BindingFlags.Instance)!.SetValue(manifest, true);
            }

            var banned = localPlugin.GetType().GetProperty("IsBanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin);
            if ((bool)banned!) 
                localPlugin.GetType().GetField("<IsBanned>k__BackingField", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.SetValue(localPlugin, false);
            /*banned = localPlugin.GetType().GetProperty("IsBanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin);
            PluginLog.Warning($"{localPlugin.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin)} / {banned}");*/
        }
    }
}