using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ExposedObject;

namespace MyPluginsArentCheat;

// Credit goes to NightmareXiv

public class EntryPoint : IDalamudPlugin
{
    private readonly object _pluginManager;
    private readonly Type _stateEnum;
    
    public EntryPoint([RequiredVersion("1.0")] DalamudPluginInterface pi)
    {
         _pluginManager = Exposed.From(pi.GetType().Assembly.
                                            GetType("Dalamud.Service`1", true)
                                           .MakeGenericType(pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)))
                                   .Get();
        _stateEnum = pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.Types.PluginState");
        
        UnbanInstalledPlugins();
    }
    
    private void UnbanInstalledPlugins()
    {
        var installedPlugins = Exposed.From(_pluginManager).InstalledPlugins;
        foreach (var plugin in installedPlugins)
        {
            var localPlugin = (object)plugin;
            var state = localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin).ToString();
            if (state == "LoadError")
            {
                localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance).SetValue(localPlugin, _stateEnum.GetEnumValues().GetValue(0));
                var manifest = localPlugin.GetType().GetProperty("Manifest", BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin);
                manifest.GetType().GetProperty("Disabled", BindingFlags.Public | BindingFlags.Instance).SetValue(manifest, true);
            }

            var banned = localPlugin.GetType().GetProperty("IsBanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin);
            if ((bool)banned)
            {
                localPlugin.GetType().GetField("<IsBanned>k__BackingField", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).SetValue(localPlugin, false);
            }
            banned = localPlugin.GetType().GetProperty("IsBanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(localPlugin);
        }
    }

    public string Name => "MyPluginsArentCheat";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}