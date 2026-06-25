Imports System.Linq
Imports System.Reflection.Emit
Imports BepInEx
Imports DarkMachine.UI
Imports HarmonyLib
Imports UnityEngine

Namespace MoreFOV

    <BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)>
    Public Class Plugin
        Inherits BaseUnityPlugin

        ''' <summary> Patch all the <see cref="HarmonyPatch"/>'s in this class, and set the mods <see cref="Harmony"/> instance(<seealso cref="PluginInfo.Harm"/>). </summary>
        Public Sub Awake()
            PluginInfo.Harm = Harmony.CreateAndPatchAll(Me.GetType(), PluginInfo.GUID)
        End Sub

        ''' <summary> Edits the fov menu to add more fov Or smt idk miaow meow meow meow :3 &gt;///&lt; </summary>
        <HarmonyPostfix> <HarmonyPatch(GetType(UI_SettingsMenu), "Start")>
        Public Shared Sub EditFOV(__instance As UI_SettingsMenu)

            Dim SettingsMenuTrans = __instance.transform ' transgener >w< '
            Dim FOVSliderTrans = SettingsMenuTrans.Find("SettingsParent/Settings Pane/Video Settings/Main Panel/Tab - Video/Column - Video/SliderAsset - FOV/Slider")

            ' if the slider is not found, search for it in all children (just redundancy cuz i dont want to update the mod every update) '
            If (FOVSliderTrans Is Nothing) Then
                FOVSliderTrans = SettingsMenuTrans.GetComponentsInChildren(Of Transform)(True).FirstOrDefault(
                    Function(child)

                        ' if the slider is a child of a parent with "FOV" in its name, its probably the fov slider :3 '
                        If (child.name.Contains("Slider") AndAlso child.parent.name.Contains("FOV")) Then

                            ' theres old sliders that dont have a SubmitSlider component, so we check for that too '
                            If (child.GetComponent(Of SubmitSlider)() IsNot Nothing) Then
                                Return True
                            End If

                        End If

                        Return False

                    End Function)
            End If

            Dim FOVSlider = FOVSliderTrans.GetComponent(Of SubmitSlider)()
            FOVSlider.maxValue = 180.0F
            FOVSlider.minValue = 0F

            ' reset the value because UI_SettingsMenu only has a start method '
            ' And in Awake/OnEnable the submit slider clamps the value b4 we set the New max '
            FOVSlider.value = SettingsManager.settings.playerFOV
        End Sub

        ''' <summary> Edits the CIL instructions of ENT_Player.FixedUpdate to prevent it from clamping the fov 3 </summary>
        <HarmonyTranspiler> <HarmonyPatch(GetType(ENT_Player), "FixedUpdate")>
        Public Shared Function DontClampFOV(instructions As IEnumerable(Of CodeInstruction)) As IEnumerable(Of CodeInstruction)

            Dim Mathf_Clamp = AccessTools.Method(GetType(Mathf), "Clamp", New Type() {GetType(Single), GetType(Single), GetType(Single)})
            Dim Player_curFOV = AccessTools.Field(GetType(ENT_Player), "curFOV")

            Dim result = New List(Of CodeInstruction)
            Dim instructionsArr = instructions.ToArray()
            For i = 0 To instructionsArr.Length - 1

                Dim instruction = instructionsArr(i)

                ' if the current instruction Is clamping 2 floats on the evaluation stack '
                ' And the next instruction sets ENT_Player.curFOV to the result '
                ' then replace the min And max params with our own :3 '
                If instruction.Calls(Mathf_Clamp) AndAlso instructionsArr(i + 1).Is(OpCodes.Stfld, Player_curFOV) Then

                    result.Add(New CodeInstruction(OpCodes.Pop)) ' pop off `140f` from the evaluation stack '
                    result.Add(New CodeInstruction(OpCodes.Pop)) ' do the same for `60f` '

                    result.Add(New CodeInstruction(OpCodes.Ldc_R4, 0F)) ' put '0f' in place of '60f' (min) '
                    result.Add(New CodeInstruction(OpCodes.Ldc_R4, 180.0F)) ' '180f' in place of '140f' (max) '

                    ' continue as normal since we dont have to change anything else'
                    ' Mathf.Clamp will be called with the parameters `(0f, 180f)` instead of `(60f, 140f)` which Is exactly what we wanted '

                End If

                ' else Or after we've edited stuff, just return the original instruction '
                result.Add(instruction)

            Next

            Return result

        End Function

    End Class

End Namespace