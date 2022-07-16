using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace DailyAdofai {
    #if DEBUG
    [EnableReloading]
    #endif
    public class Main {
        private static UnityModManager.ModEntry _mod;
        public static Harmony Harmony;
        public static Settings Settings;

        private static bool Load(UnityModManager.ModEntry modEntry) {
            _mod = modEntry;
            _mod.OnToggle = OnToggle;
            //_mod.OnGUI = OnGUI;
            //_mod.OnSaveGUI = OnSaveGUI;
            
            #if DEBUG
            _mod.OnUnload = Unload;
            #endif

            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            Harmony = new Harmony(_mod.Info.Id);

            Assets.Load(_mod.Path);
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            if (value) Harmony.PatchAll(Assembly.GetExecutingAssembly());
            else Harmony.UnpatchAll(_mod.Info.Id);
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            Settings.Save(modEntry);
        }
        
        #if DEBUG
        static bool Unload(UnityModManager.ModEntry modEntry) {
            Harmony.UnpatchAll(_mod.Info.Id);
            
            return true;
        }
        #endif
    }
}