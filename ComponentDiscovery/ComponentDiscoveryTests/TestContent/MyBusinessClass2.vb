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
