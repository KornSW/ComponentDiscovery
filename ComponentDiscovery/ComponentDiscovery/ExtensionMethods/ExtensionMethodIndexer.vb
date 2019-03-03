'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection

Public Class ExtensionMethodIndexer

  Private _CachesPerExtendeeType As New Dictionary(Of Type, MethodIndex)
  Private _AssemblyIndexer As IAssemblyIndexer

  Public Sub New(assemblyIndexer As IAssemblyIndexer)
    _AssemblyIndexer = assemblyIndexer
    _AssemblyIndexer.SubscribeForAssemblyApproved(AddressOf Me.AddAssembly)
  End Sub

  Public Sub AddAssembly(assembly As Assembly)
    Dim allCaches As MethodIndex()
    SyncLock _CachesPerExtendeeType
      allCaches = _CachesPerExtendeeType.Values.ToArray()
    End SyncLock
    For Each cache In allCaches
      cache.AddAssembly(assembly)
    Next
  End Sub

  Public Function GetApplicableExtensionMethods(Of TExtendee)() As IEnumerable(Of MethodInfo)
    Return Me.GetApplicableExtensionMethods(GetType(TExtendee))
  End Function

  Public Function GetApplicableExtensionMethods(extendeeType As Type) As IEnumerable(Of MethodInfo)
    Return Me.GetCache(extendeeType)
  End Function

  Private Function GetCache(extendeeType As Type) As MethodIndex

    SyncLock _CachesPerExtendeeType
      If (_CachesPerExtendeeType.ContainsKey(extendeeType)) Then
        Return _CachesPerExtendeeType(extendeeType)
      End If
    End SyncLock

    'ACHTUNG: darf nicht innerhalb des SyncLock sein, da der recursive aufruf von GetCache sonst blockiert wäre
    Dim cache As New MethodIndex(extendeeType, AddressOf Me.GetCache)

    SyncLock _CachesPerExtendeeType
      _CachesPerExtendeeType.Add(extendeeType, cache)
    End SyncLock

    For Each approvedAssembly In _AssemblyIndexer.ApprovedAssemblies
      'If (extendeeType.Assembly.IsDirectReferencedBy(approvedAssembly)) Then
      cache.AddAssembly(approvedAssembly)
      'End If
    Next

    Return cache
  End Function

End Class
