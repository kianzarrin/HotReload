using Harmony;
using HotReload.Utils;

namespace HotReload.Patches
{
    public class HarmonyExtension
    {
        HarmonyInstance harmony;
        public const string HARMONY_ID = "CS.Kian.HotReload";

        public void InstallHarmony()
        {
#if !DEBUG
            // TODO: this does not work because Before OnCreate we don't know if we are in asset editor.
            if (Extensions.InAssetEditor) {
                Log.Info("skipped InstallHarmony in asset editor release build");
                return;
            }
#endif
            if (harmony == null)
            {
                Log.Info("HotReload Patching...");
#if DEBUG
                HarmonyInstance.DEBUG = true;
#endif
                harmony = HarmonyInstance.Create(HARMONY_ID);
                harmony.PatchAll(GetType().Assembly);
            }
        }

        public void UninstallHarmony()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(HARMONY_ID);
                harmony = null;
                Log.Info("HotReload patches Reverted.");
            }
        }
    }
}