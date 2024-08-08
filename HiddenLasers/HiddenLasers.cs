using System.Collections.Generic;
using System.Linq;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace HiddenLasers
{
    public class Patch : ResoniteMod
    {
        public override string Name => "HiddenLasers";
        public override string Author => "hazre, NepuShiro";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/hazre/HiddenLasers";

        public static ModConfiguration config;

        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> LASER_VISIBLE = new ModConfigurationKey<bool>("Should the laser be shown?", "", () => true);
        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED_DESKTOP = new ModConfigurationKey<bool>("Should the laser be shown in Desktop Mode?", "", () => false);
        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED_USERSPACE = new ModConfigurationKey<bool>("Should the laser be shown in Userspace?", "", () => true);

        public static Dictionary<InteractionLaser, FieldDrive<bool>> instances = new();

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.Save(true);

            Harmony harmony = new Harmony("dev.hazre.HiddenLasers");
            harmony.PatchAll();

            config.OnThisConfigurationChanged += (_) =>
            {
                foreach (var instance in instances.Keys)
                {
                    UpdateLaser(instance, instances[instance]);
                }
            };
        }

        private static void UpdateLaser(InteractionLaser instance, FieldDrive<bool> laserVisible)
        {
            if (instance == null || laserVisible == null) return;

            MeshRenderer meshRenderer = null;
            bool theBool = config.GetValue(LASER_VISIBLE);
            if (instance.Slot.World.IsUserspace()) theBool = config.GetValue(ENABLED_USERSPACE);
            if (!instance.Slot.ActiveUserRoot.ActiveUser.VR_Active) theBool = config.GetValue(ENABLED_DESKTOP);

            instance.RunInUpdates(3, () =>
            {
                meshRenderer = instance.Slot.GetComponent<MeshRenderer>();

                if (theBool)
                {
                    var valField = instance.Slot.GetComponent<ValueField<bool>>() ?? instance.Slot.AttachComponent<ValueField<bool>>();
                    laserVisible.Target = valField.Value;
                    meshRenderer.Enabled = false;
                }
                else
                {
                    laserVisible.Target = meshRenderer.EnabledField;
                }
            });
        }

        [HarmonyPatch(typeof(InteractionLaser))]
        class PatchInteractionLaser
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnAwake")]
            static void Postfix(InteractionLaser __instance, FieldDrive<bool> ____laserVisible)
            {
                if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser)
                    return;

                __instance.RunInUpdates(3, () =>
                {
                    instances = instances.Where(kvp => kvp.Key != null && kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (!instances.TryGetValue(__instance, out _)) instances.Add(__instance, ____laserVisible);

                    if (__instance.Slot.GetComponent<ValueField<bool>>() == null) __instance.Slot.AttachComponent<ValueField<bool>>();

                    UpdateLaser(__instance, ____laserVisible);
                });
            }
        }
    }
}