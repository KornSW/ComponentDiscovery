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
