'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection

Namespace ClassificationDetection

  Public Class NamespaceBasedTypeClassificationDetectionStrategy
    Implements ITypeClassificationDetectionStrategy

    Public Sub New()
    End Sub

    Public Overridable Function TryDetectClassificationsForType(
      t As Type, taxonomicDimensionName As String, ByRef classifications As String()
    ) As Boolean Implements ITypeClassificationDetectionStrategy.TryDetectClassificationsForType
      classifications = {t.Namespace}
      Return True
    End Function

  End Class

End Namespace
