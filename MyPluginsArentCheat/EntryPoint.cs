using System;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Plugin;
using ExposedObject;

namespace MyPluginsArentCheat;

// Credit goes to NightmareXiv for awesome ExposedObject in their UnloadErrorFuckoff

public class EntryPoint : IDalamudPlugin
{
    private readonly object _pluginManager;
    private readonly Type _stateEnum;

    public EntryPoint(IDalamudPluginInterface pi)
    {
        _pluginManager = Exposed.From(pi.GetType().Assembly.GetType("Dalamud.Service`1", true)!.MakeGenericType(pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)!))
                                .Get();

        _stateEnum = pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.Types.PluginState");

        RemoveBannedPlugins();
        UnbanInstalledPlugins();
        NoMeasurementYo();
    }

    public string Name => "MyPluginsArentCheat";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    internal static object GetService(string name)
    {
        return typeof(IDalamudPlugin).Assembly.GetType("Dalamud.Service`1")!.MakeGenericType(typeof(IDalamudPlugin).Assembly.GetType(name)!)
                                     .GetMethod("Get", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null)!;
    }

    private void RemoveBannedPlugins()
    {
        var bannedPlugins = _pluginManager.GetType().GetField("bannedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var arrayType = bannedPlugins.FieldType.GetElementType();
        var emptyArray = Array.CreateInstance(arrayType, 0);
        bannedPlugins.SetValue(_pluginManager, emptyArray);
    }

    private async void UnbanInstalledPlugins()
    {
        var installedPlugins = Exposed.From(_pluginManager).InstalledPlugins;
        foreach (var plugin in installedPlugins)
        {
            var localPlugin = (object)plugin;
            var state = localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin)?.ToString();
            if (state == "LoadError")
            {
                localPlugin.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.SetValue(localPlugin, _stateEnum.GetEnumValues().GetValue(0));
                var loadMethod = localPlugin.GetType().GetMethod("LoadAsync");
                if (loadMethod != null)
                {
                    await (Task) loadMethod.Invoke(localPlugin, [3, false]);
                }
            }

            var banned = localPlugin.GetType().GetProperty("IsBanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin);
            if ((bool)banned!)
                localPlugin.GetType().GetField("<IsBanned>k__BackingField", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.SetValue(localPlugin, false);
        }
    }

    private static void NoMeasurementYo()
    {
        var chatHandler = GetService("Dalamud.Game.ChatHandlers");
        chatHandler.SetFieldValue("hasSendMeasurement", true);
    }
}