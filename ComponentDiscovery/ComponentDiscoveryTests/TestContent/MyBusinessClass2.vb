'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports ComponentDiscoveryTests

Public Class MyBusinessClass2
  Implements MyBusinessInterfaceA

  Public Sub New(foo As Boolean)
  End Sub

  Public Function GetDemoValue() As String Implements MyBusinessInterfaceA.GetDemoValue
    Return "Demo-Value-2"
  End Function

End Class
