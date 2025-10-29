using HarmonyLib;
using MelonLoader;
using MyBhapticsTactsuit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: MelonInfo(typeof(ArkenAge_bhaptics.ArkenAge_bhaptics), "ArkenAge_bhaptics", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("VitruviusVR", "Arken Age")]

namespace ArkenAge_bhaptics
{
    public class ArkenAge_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        /*
        [HarmonyPatch(typeof(PlayerActor), "UpdateLowHealthState", new Type[] { })]
        public class bhaptics_PlayerHealth
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerActor __instance)
            {

            }
        }
        */
    }
}
