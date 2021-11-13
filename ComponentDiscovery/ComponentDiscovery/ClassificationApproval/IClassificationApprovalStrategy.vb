'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Namespace ComponentDiscovery.ClassificationApproval

  ''' <summary>
  ''' A 'ClassificationApprovalStrategy' opposes classifications with clearances and returns true if conformity is given.
  ''' </summary>
  Public Interface IClassificationApprovalStrategy

    ''' <summary>
    '''   Opposes classifications with clearances and returns true if conformity is given.
    ''' </summary>
    Function VerifyTarget(classificationExpressions As String(), clearanceExpressions As String(), taxonomicDimensionName As String) As Boolean

  End Interface

End Namespace
