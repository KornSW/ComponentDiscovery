'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace ClassificationDetection

  Public Interface ITypeClassificationDetectionStrategy

    Function TryDetectClassificationsForType(t As Type, taxonomicDimensionName As String, ByRef classifications As String()) As Boolean

  End Interface

End Namespace
