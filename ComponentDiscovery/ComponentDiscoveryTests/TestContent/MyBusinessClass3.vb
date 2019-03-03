'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports ComponentDiscovery
Imports ComponentDiscoveryTests

<TypeClassification("BusinessConcern", "ConcernC")>
Public Class MyBusinessClass3
  Implements MyBusinessInterfaceA

  Public Sub New(foo As Boolean)
  End Sub

  Public Function GetDemoValue() As String Implements MyBusinessInterfaceA.GetDemoValue
    Return "Demo-Value-3"
  End Function

End Class
