Imports HarmonyLib

Namespace MoreFOV

    ''' <summary> Basic information about the Mod. </summary>
    Public Module PluginInfo

        Public Const GUID As String = "Bryan_-000-.MoreFOV"
        Public Const Name As String = "MoreFOV"
        Public Const Version As String = "1.0.0"

        Private _Harm As Harmony ' Visual Basic just doesn't have explicit mixed access level property gen so :P '
        Public Property Harm As Harmony

            Get
                Return _Harm
            End Get

            Friend Set(value As Harmony)
                _Harm = value
            End Set

        End Property

    End Module

End Namespace