using System;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ExposedObject;

namespace MyPluginsArentCheat;

// Credit goes to NightmareXiv for awesome ExposedObject in their UnloadErrorFuckoff

public class EntryPoint : IDalamudPlugin
{
    private readonly object                  _pluginManager;
    private readonly Type                    _stateEnum;
    private readonly IClientState            _clientState;
    private readonly IFramework              _framework;
    private readonly IPluginLog              _logger;
    private readonly IDalamudPluginInterface _pi;

    public EntryPoint(IDalamudPluginInterface pi, IClientState clientState, IFramework framework, IPluginLog log)
    {
        _clientState = clientState;
        _framework   = framework;
        _logger      = log;
        _pi          = pi;

        _pluginManager = Exposed.From(pi.GetType().Assembly.GetType("Dalamud.Service`1", true)!.MakeGenericType(pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)!))
                                .Get();

        _stateEnum = pi.GetType().Assembly.GetType("Dalamud.Plugin.Internal.Types.PluginState");

        RemoveBannedPlugins();
        Task.Run(UnbanInstalledPlugins);

        NoMeasurementYo();
        _clientState.Logout += OnLogout;
    }

    private void OnLogout(int type, int code)
    {
        _framework.RunOnTick(NoMeasurementYo, TimeSpan.FromMilliseconds(50));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _clientState.Logout -= OnLogout;
    }

    private static object GetService(string name)
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

    private async Task UnbanInstalledPlugins()
    {
        var installedPlugins = Exposed.From(_pluginManager).InstalledPlugins;

        foreach (var plugin in installedPlugins)
        {
            object localPlugin = plugin;
            var    pluginType  = localPlugin.GetType();

            var state = pluginType.GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.GetValue(localPlugin)
                                  ?.ToString();

            if (string.IsNullOrWhiteSpace(state))
            {
                continue;
            }

            var isBanned = (bool) (pluginType
                                   .GetProperty("IsBanned",
                                                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                   ?.GetValue(localPlugin)
                                   ?? false);

            var isWantedByAnyProfile
                = (bool) (pluginType.GetProperty("IsWantedByAnyProfile", BindingFlags.Instance | BindingFlags.Public)
                                    ?.GetValue(plugin)
                          ?? false);

            if (!isBanned)
            {
                continue;
            }

            if (pluginType.Name != "LocalDevPlugin")
            {
                pluginType
                    .GetField("<IsBanned>k__BackingField",
                              BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(localPlugin, false);
            }
            else
            {
                pluginType.BaseType?.GetField("<IsBanned>k__BackingField",
                                              BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                          ?.SetValue(localPlugin, false);
            }

            var correctState = state.ToLowerInvariant() is "loaderror" or "unloaded";

            if (!correctState)
            {
                continue;
            }

            if (!isWantedByAnyProfile)
            {
                continue;
            }

            try
            {
                pluginType.GetProperty("State", BindingFlags.Public | BindingFlags.Instance)
                          ?.SetValue(localPlugin, _stateEnum.GetEnumValues().GetValue(0));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when trying to set state!!!");
            }

            try
            {
                var loadMethod = localPlugin.GetType().GetMethod("LoadAsync");

                if (loadMethod != null)
                {
                    await (Task) loadMethod.Invoke(localPlugin, [3, false]);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error when trying to invoke loadmethod");
            }
        }
    }

    private void NoMeasurementYo()
    {
    }
}