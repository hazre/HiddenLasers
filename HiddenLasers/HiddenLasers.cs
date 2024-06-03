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

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.Save(true);

            Harmony harmony = new Harmony("me.hazre.HiddenLasers");
            harmony.PatchAll();

        }

        [HarmonyPatch(typeof(InteractionLaser))]
        class PatchInteractionLaser
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnAwake")]
            static void Postfix(InteractionLaser __instance, FieldDrive<bool> ____laserVisible, FieldDrive<bool> ____directCursorVisible)
            {
                __instance.RunInUpdates(3, () =>
                {
                    if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser)
                        return;

                    Slot slot = __instance.Slot;
                    var temp = slot.AttachComponent<ValueField<bool>>();
                    var laser = slot.GetComponent<MeshRenderer>().Mesh;
                    var directCursor = slot.FindChild("DirectCursor");
                    ____laserVisible.ForceLink(temp.Value);
                    ____directCursorVisible.ForceLink(temp.Value);


                    laser.Component.Enabled = config.GetValue(LASER_VISIBLE);
                    directCursor.ActiveSelf_Field.Value = config.GetValue(LASER_VISIBLE);
                });
            }
        }
    }
}