'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace ComponentDiscovery.ClassificationDetection

  ''' <summary>
  ''' Reads Classifications from a Manifest-File named right to the assembly file but with file-extension '.cl'.
  ''' The Format must be JSON in the form: {
  '''   "componentDiscovery": {
  '''     "dimensionNameA": "SingleExpression",
  '''     "dimensionNameB : ["OneExpression", "AnotherExpression"]
  '''   } 
  ''' }
  ''' </summary>
  Public Class ManifestBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    Public Sub New(ParamArray defaultsIfNoAttributeFound As String())

      _DefaultsIfNoAttributeFound = defaultsIfNoAttributeFound

      If (_DefaultsIfNoAttributeFound Is Nothing) Then
        _DefaultsIfNoAttributeFound = {}
      End If

    End Sub

    Public ReadOnly Property DefaultsIfNoAttributeFound As String()
      Get
        Return _DefaultsIfNoAttributeFound
      End Get
    End Property

    Public Function TryDetectClassificationsForAssembly(
      assemblyFullFilename As String,
      taxonomicDimensionName As String,
      ByRef classifications As String()
    ) As Boolean Implements IAssemblyClassificationDetectionStrategy.TryDetectClassificationsForAssembly

      Dim assemblyFileContainingFolder As String = Path.GetDirectoryName(assemblyFullFilename)
      Dim assemblyFileWoExt As String = Path.GetFileNameWithoutExtension(assemblyFullFilename)
      Dim manifestFileFullName As String = Path.Combine(assemblyFileContainingFolder, assemblyFileWoExt + ".cl")

      If (Not File.Exists(manifestFileFullName)) Then
        Diag.Verbose(Function() $"Manifest-File '{manifestFileFullName}' doesnt exist, returning defaults: {String.Join(",", Me.DefaultsIfNoAttributeFound)}")
        classifications = Me.DefaultsIfNoAttributeFound
        Return True
      End If

      Try

        Dim rawJsonContent As String
        Using fs As New FileStream(manifestFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read)
          Using sr As New StreamReader(fs, Encoding.UTF8)
            rawJsonContent = sr.ReadToEnd()
          End Using
        End Using

        Dim collector As New List(Of String)
        Dim dimensionIsDefined As Boolean = False
        ManifestParser.Parse(
         rawJsonContent,
         Sub(dimensionName As String, classificationExpression As String, namespaceIncludePatterns As String(), namespaceExludePatterns As String())
           If (String.Equals(dimensionName, taxonomicDimensionName, StringComparison.InvariantCultureIgnoreCase)) Then
             dimensionIsDefined = True
             collector.Add(classificationExpression)
           End If
         End Sub,
         Sub(emptyDimensionName As String)
           If (String.Equals(emptyDimensionName, taxonomicDimensionName, StringComparison.InvariantCultureIgnoreCase)) Then
             'explicit empty!
             dimensionIsDefined = True
           End If
         End Sub
       )
        If (dimensionIsDefined) Then
          classifications = collector.ToArray()
        Else
          Diag.Verbose(Function() $"Manifest-File '{manifestFileFullName}' doesnt contain information for dimension '{taxonomicDimensionName}', returning defaults: {String.Join(",", Me.DefaultsIfNoAttributeFound)}")
          classifications = Me.DefaultsIfNoAttributeFound
        End If

        Return True
      Catch ex As Exception
        Diag.Error($"AssemblyIndexer: Exception in '{NameOf(ManifestBasedAssemblyClassificationDetectionStrategy)}.{NameOf(TryDetectClassificationsForAssembly)}()': {ex.Message}")
        Diag.Verbose(Function() ex.StackTrace)
        Return False
      End Try

    End Function

  End Class

End Namespace
