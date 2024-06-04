using System.Collections.Generic;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace HiddenLasers
{
    public class Patch : ResoniteMod
    {
        public override string Name => "HiddenLasers";
        public override string Author => "hazre";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/hazre/HiddenLasers/";

        public static ModConfiguration config;

        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> LASER_VISIBLE = new ModConfigurationKey<bool>("Should the laser be shown?", "", () => true);
        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED_DESKTOP = new ModConfigurationKey<bool>("Should the laser be shown in Desktop Mode?", "", () => false);
        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED_USERSPACE = new ModConfigurationKey<bool>("Should the laser be shown in Userspace?", "", () => true);

        public static List<InteractionLaser> lasers = new List<InteractionLaser>();

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.Save(true);

            Harmony harmony = new Harmony("me.hazre.HiddenLasers");
            harmony.PatchAll();

            config.OnThisConfigurationChanged += (_) =>
            {
                foreach (var instance in lasers)
                {
                    if (instance != null)
                    {
                        UpdateLaser(instance);
                    }
                }
            };
        }

        public static void UpdateLaser(InteractionLaser __instance)
        {
            var laserVisible = config.GetValue(LASER_VISIBLE);
            if (__instance.Slot.World.IsUserspace()) laserVisible = config.GetValue(ENABLED_USERSPACE);
            if (!__instance.Slot.ActiveUserRoot.ActiveUser.VR_Active) laserVisible = config.GetValue(ENABLED_DESKTOP);
            var laser = __instance.Slot.GetComponent<MeshRenderer>().Mesh;
            var directCursor = __instance.Slot.FindChild("DirectCursor");

            laser.Component.Enabled = laserVisible;
            directCursor.ActiveSelf_Field.Value = laserVisible;
        }

        public static void ForceLinkTemp(InteractionLaser __instance, FieldDrive<bool> ____laserVisible, FieldDrive<bool> ____directCursorVisible)
        {
            var temp = __instance.Slot.AttachComponent<ValueField<bool>>();
            var temp2 = __instance.Slot.AttachComponent<ValueField<bool>>();
            ____laserVisible.ForceLink(temp.Value);
            ____directCursorVisible.ForceLink(temp2.Value);
        }

        [HarmonyPatch(typeof(InteractionLaser))]
        class PatchInteractionLaser
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnAttach")]
            static void OnAttach(InteractionLaser __instance, FieldDrive<bool> ____laserVisible, FieldDrive<bool> ____directCursorVisible)
            {
                if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser)
                    return;

                ForceLinkTemp(__instance, ____laserVisible, ____directCursorVisible);
                UpdateLaser(__instance);

                lasers.RemoveAll(instance => instance == null);
                if (!lasers.Contains(__instance)) lasers.Add(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(ConnectorManager), "ThreadCheck")]
    public class DisableThreadCheck
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}