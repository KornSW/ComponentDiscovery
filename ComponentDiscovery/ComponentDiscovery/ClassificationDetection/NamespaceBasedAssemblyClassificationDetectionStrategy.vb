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

  Public Class NamespaceBasedAssemblyClassificationDetectionStrategy
    Inherits AttributeBasedAssemblyClassificationDetectionStrategy

    Public Sub New()
      MyClass.New(True, False)
    End Sub

    Public Sub New(enableAnalysisSandbox As Boolean, enablePersistentCache As Boolean)
      MyBase.New(enableAnalysisSandbox, enablePersistentCache, {})
    End Sub

    Protected Overrides ReadOnly Property FetchMethod As Func(Of String, String, String())
      Get
        Return AddressOf FetchNamespaceFromAssembly
      End Get
    End Property

    Private Shared Function FetchNamespaceFromAssembly(assemblyFullFilename As String, dimensionName As String) As String()
      Try
        Dim assemblyToAnalyze = Assembly.LoadFile(assemblyFullFilename)
        If (assemblyToAnalyze IsNot Nothing) Then
          Dim defaultNs As String = GetDefaultNamespaceOfAssembly(assemblyToAnalyze)
          Return {defaultNs}
        End If
      Catch ex As BadImageFormatException 'non-.NET-dll
        'EXPECTED: happens on non-.NET-dll
      Catch ex As Exception
        Diag.Error(ex)
      End Try
      Return Nothing '=ERROR
    End Function

    Private Shared Function GetDefaultNamespaceOfAssembly(assembly As Assembly) As String
      Dim referenceType As Type =
        (From t As Type In GetAccessableTypesFromAssembly(assembly) Where t.FullName.Contains(".My.")).FirstOrDefault()
      If (referenceType Is Nothing) Then
        Return String.Empty
      Else
        Return referenceType.FullName.Substring(0, referenceType.FullName.IndexOf(".My."))
      End If
    End Function

    Private Shared Function GetAccessableTypesFromAssembly(assembly As Assembly) As Type()
      Try
        Return assembly.GetTypes()
      Catch ex As ReflectionTypeLoadException
        'This ugly workarround is the only way to get the types from a asembly
        'which contains one or more types with broken references f.e:
        '  [Type1] good!
        '  [Type2] good!
        '  [Type3] INHERITS [<non-exitings-assembly>.BaseType3] bad!
        Return ex.Types().Where(Function(t) t IsNot Nothing).ToArray()
      End Try
    End Function

  End Class

End Namespace
