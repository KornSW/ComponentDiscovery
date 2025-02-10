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
Imports System.Runtime
Imports System.Threading
Imports System.Threading.Tasks
Imports dnlib.DotNet

Namespace ComponentDiscovery.ClassificationDetection

  Public Class DnLibBasedAssemblyClassificationDetectionStrategy
    Implements IAssemblyClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _DefaultsIfNoAttributeFound As String()

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _InMemoryCachedAssemblies As Integer = 10

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

      Try

        Dim attribs = GetDnLibAssemblyAttributes(assemblyFullFilename)

        If (attribs Is Nothing) Then
          'ERROR (BadImageFormatException)
          Return False
        End If

        classifications = attribs.Where(
          Function(a) a.ConstructorArguments(0).Value.ToString().Equals(taxonomicDimensionName, StringComparison.CurrentCultureIgnoreCase)
        ).Select(Function(a) a.ConstructorArguments(1).Value.ToString()).ToArray()

        If (classifications.Length = 0) Then
          classifications = _DefaultsIfNoAttributeFound
        End If

        Return True
      Catch ex As Exception
        Return False
      End Try

    End Function

    Private Shared _RollingCache As New List(Of Tuple(Of String, CustomAttribute()))

    Private Shared Function GetDnLibAssemblyAttributes(assemblyFullFilename As String) As CustomAttribute()

      If (_InMemoryCachedAssemblies > 0) Then

        Dim cacheEntry As Tuple(Of String, CustomAttribute()) = Nothing

        SyncLock (_RollingCache)

          For Each entry In _RollingCache
            If (entry.Item1.Equals(assemblyFullFilename, StringComparison.CurrentCultureIgnoreCase)) Then
              cacheEntry = entry
              Exit For
            End If
          Next

          If (cacheEntry IsNot Nothing) Then
            _RollingCache.Remove(cacheEntry)
            _RollingCache.Insert(0, cacheEntry)
            Return cacheEntry.Item2
          End If

        End SyncLock

      End If

      Dim newCacheEntry As Tuple(Of String, CustomAttribute())

      Try
        Dim aDef = AssemblyDef.Load(assemblyFullFilename)

        Dim attribs = aDef.CustomAttributes.Where(
         Function(a) a.TypeFullName = GetType(AssemblyMetadataAttribute).FullName
        ).ToArray()

        newCacheEntry = New Tuple(Of String, CustomAttribute())(
          assemblyFullFilename, attribs
        )

      Catch ex As BadImageFormatException
        'die assembly wird niemals ladbar sein -> ergebnis cachen
        newCacheEntry = New Tuple(Of String, CustomAttribute())(
          assemblyFullFilename, Nothing
        )
      Catch ex As Exception
        'bei zugriffsproblemen das ergebnis nicht cachen
        Return Nothing
      End Try

      If (_InMemoryCachedAssemblies > 0) Then
        SyncLock (_RollingCache)
          For i As Integer = _RollingCache.Count - 1 To _InMemoryCachedAssemblies Step -1
            _RollingCache.RemoveAt(i)
          Next
          _RollingCache.Insert(0, newCacheEntry)
        End SyncLock
      End If

      Return newCacheEntry.Item2
    End Function

  End Class

End Namespace
