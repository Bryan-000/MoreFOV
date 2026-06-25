using System.Linq;

namespace MoreFOV;

using BepInEx;
using DarkMachine.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    /// <summary> Basic information about the mod. </summary>
    public static class PluginInfo
    {
        public const string GUID = "Bryan_-000-.MoreFOV";
        public const string Name = "MoreFOV";
        public const string Version = "1.0.0";

        public static Harmony Harm { get; internal set; }
    }

    /// <summary> Patch all the <see cref="HarmonyPatch"/>'s in this class, and set the mods <see cref="Harmony"/> instance(<seealso cref="PluginInfo.Harm"/>). </summary>
    public void Awake() =>
        PluginInfo.Harm = Harmony.CreateAndPatchAll(GetType(), PluginInfo.GUID);

    /// <summary> Edits the fov menu to add more fov or smt idk miaow meow meow meow :3 &gt;///&lt; </summary>
    [HarmonyPostfix] [HarmonyPatch(typeof(UI_SettingsMenu), "Start")]
    public static void EditFOV(UI_SettingsMenu __instance)
    {
        Transform SettingsMenuTrans = __instance.transform; // transgener >w<
        Transform FOVSliderTrans = SettingsMenuTrans.Find("SettingsParent/Settings Pane/Video Settings/Main Panel/Tab - Video/Column - Video/SliderAsset - FOV/Slider");

        // if the slider is not found, search for it in all children (just redundancy cuz i dont want to update the mod every update)
        if (!FOVSliderTrans)
        {
            FOVSliderTrans = SettingsMenuTrans.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child =>
                {
                    return child.name.Contains("Slider") // if the slider is a child of a parent with "FOV" in its name, its probably the fov slider :3
                           && child.parent.name.Contains("FOV")
                           && child.GetComponent<SubmitSlider>() != null; // theres old sliders that dont have a SubmitSlider component, so we check for that too
                });
        }

        SubmitSlider FOVSlider = FOVSliderTrans.GetComponent<SubmitSlider>();
        FOVSlider.maxValue = 180f;
        FOVSlider.minValue = 0f;

        // reset the value because UI_SettingsMenu only has a start method
        // and in Awake/OnEnable the submit slider clamps the value b4 we set the new max
        FOVSlider.value = SettingsManager.settings.playerFOV;
    }

    /// <summary> Edits the CIL instructions of ENT_Player.FixedUpdate to prevent it from clamping the fov :3 </summary>
    [HarmonyTranspiler] [HarmonyPatch(typeof(ENT_Player), "FixedUpdate")]
    public static IEnumerable<CodeInstruction> DontClampFOV(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo Mathf_Clamp = AccessTools.Method(typeof(Mathf), "Clamp", [typeof(float), typeof(float), typeof(float)]);
        FieldInfo Player_curFOV = AccessTools.Field(typeof(ENT_Player), "curFOV");

        CodeInstruction[] instructionsArr = [.. instructions];
        for (int i = 0; i < instructionsArr.Length; i++)
        {
            CodeInstruction instruction = instructionsArr[i];

            // if the current instruction is clamping 2 floats on the evaluation stack
            // and the next instruction sets ENT_Player.curFOV to the result
            // then replace the min and max params with our own :3
            if (instruction.Calls(Mathf_Clamp) && instructionsArr[i + 1].Is(OpCodes.Stfld, Player_curFOV))
            {
                yield return new(OpCodes.Pop); // pop off `140f` from the evaluation stack
                yield return new(OpCodes.Pop); // do the same for `60f`

                yield return new(OpCodes.Ldc_R4, 0f); // put '0f' in place of '60f' (min)
                yield return new(OpCodes.Ldc_R4, 180f); // put '180f' in place of '140f' (max)

                // continue as normal since we dont have to change anything else
                // Mathf.Clamp will be called with the parameters `(0f, 180f)` instead of `(60f, 140f)` which is exactly what we wanted
            }

            // else or after we've edited stuff, just return the original instruction
            yield return instruction;
        }
    }
}