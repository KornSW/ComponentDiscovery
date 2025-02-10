'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics

Namespace ComponentDiscovery.ClassificationDetection

  Public Class DelegateBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Private _ClearanceExpressions As New List(Of String)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _AssemblyAnalyzerMethod As Func(Of String, List(Of String), Boolean) = Nothing

    ''' <summary></summary>
    ''' <param name="assemblyAnalyzerMethod">this is how a assembly will be classified. You can specify a method to run your 
    ''' own code defining the string based classifications for a given assembly file name)</param>
    Public Sub New(assemblyAnalyzerMethod As Func(Of String, List(Of String), Boolean))
      _AssemblyAnalyzerMethod = assemblyAnalyzerMethod
    End Sub

    Public ReadOnly Property AssemblyAnalyzerMethod As Func(Of String, List(Of String), Boolean)
      Get
        Return _AssemblyAnalyzerMethod
      End Get
    End Property

    Public Function TryDetectClassificationsForAssembly(
      assemblyFullFilename As String, taxonomicDimensionName As String, ByRef classifications As String()
    ) As Boolean Implements IAssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly

      Dim buffer As New List(Of String)

      If (_AssemblyAnalyzerMethod.Invoke(assemblyFullFilename, buffer)) Then
        classifications = buffer.ToArray()
        Return True
      End If

      Return False
    End Function

  End Class

End Namespace
