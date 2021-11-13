'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Namespace ComponentDiscovery.ClassificationDetection

  Public Interface IAssemblyClassificationDetectionStrategy

    Function TryDetectClassificationsForAssembly(
      assemblyFullFilename As String, taxonomicDimensionName As String, ByRef classifications As String()
    ) As Boolean

  End Interface

End Namespace
