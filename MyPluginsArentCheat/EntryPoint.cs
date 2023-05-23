using System;
using System.Linq;
using System.Reflection;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace MyPluginsArentCheat;

public class EntryPoint : IDalamudPlugin
{
    public EntryPoint([RequiredVersion("1.0")] DalamudPluginInterface pi)
    {
        var service = typeof(DalamudPluginInterface).Assembly.GetTypes().First(i => i.Name.Contains("Service`1"));
        var types = typeof(DalamudPluginInterface).Assembly.GetTypes();
        var pluginManagerType = types.First(i => i.Name == "PluginManager");
        var pluginManagerInstance = service.MakeGenericType(pluginManagerType);
        var pluginManager = pluginManagerInstance.GetMethod("Get")?.Invoke(null, null);

        var bannedPlugins = pluginManagerType.GetField("bannedPlugins", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NullReferenceException("Cannot find bannedPlugins");

        var value = (Array)bannedPlugins.GetValue(pluginManager);
        PluginLog.Warning($"Pre bannedPlugins size: {value.Length}");

        var arrayType = bannedPlugins.FieldType.GetElementType();
        var emptyArray = Array.CreateInstance(arrayType, 0);
        bannedPlugins.SetValue(pluginManager, emptyArray);
        value = (Array)bannedPlugins.GetValue(pluginManager);
        PluginLog.Warning($"Post bannedPlugins size: {value.Length}");
    }

    public string Name => "MyPluginsArentCheat";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}