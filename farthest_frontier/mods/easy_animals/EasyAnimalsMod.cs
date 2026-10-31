using MelonLoader;

[assembly: MelonInfo(typeof(EasyAnimals.EasyAnimalsMod), "Easy Animals", "0.1.0", "buffy")]
[assembly: MelonGame("Crate Entertainment", "Farthest Frontier")]

namespace EasyAnimals
{
    public class EasyAnimalsMod : MelonMod
    {
        internal static MelonLogger.Instance Log;
        internal static MelonPreferences_Entry<bool> ShowPopups;
        internal static MelonPreferences_Entry<bool> LogDiscards;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;

            // Stored in UserData/MelonPreferences.cfg, so the mod stays a single DLL for the Workshop.
            MelonPreferences_Category category = MelonPreferences.CreateCategory("EasyAnimals", "Easy Animals");
            ShowPopups = category.CreateEntry("ShowPopups", true, "Show discard popups",
                "Show an in-game notification when meat, hides or tallow is thrown away.");
            LogDiscards = category.CreateEntry("LogDiscards", false, "Log discarded items",
                "Write a console line whenever meat, hides or tallow is thrown away because it was full or at its production limit.");

            // Harmony patches in this assembly are applied automatically by MelonLoader.
            LoggerInstance.Msg("Easy Animals loaded.");
        }
    }
}
