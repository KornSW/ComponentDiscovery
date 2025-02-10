'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports ComponentDiscoveryTests

Public Class MyBusinessClass1
  Implements MyBusinessInterfaceA
  Public Sub New()
  End Sub

  Public Function GetDemoValue() As String Implements MyBusinessInterfaceA.GetDemoValue
    Return "Demo-Value-1"
  End Function

End Class
