Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace ComponentDiscovery

  Friend Class PriorityList(Of T)

    Private _ItemsByPriority As New List(Of T)
    Private _Preferences As New List(Of Tuple(Of T, T))

    Public Sub New()
      _ItemsByPriority.Add(Nothing)
    End Sub

#Region " Item-Management "

    Public Function Contains(item As T) As Boolean
      SyncLock _ItemsByPriority
        If (_ItemsByPriority.Contains(item)) Then
          Return True
        End If
      End SyncLock
      Return False
    End Function

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Public ReadOnly Property ItemsByPriority As T()
      Get
        SyncLock _ItemsByPriority
          Return _ItemsByPriority.Where(Function(i) i IsNot Nothing).ToArray()
        End SyncLock
      End Get
    End Property

#End Region

    ''' <summary>
    ''' retuns false, if the rule cant be applied because of cyclic references or
    ''' when trying to priorize fallback-items before preferred items...
    ''' throws an exception, if one of the given items is not present in the list
    ''' </summary>
    ''' <param name="higherPriorityItem">use null to address any unknown</param>
    ''' <param name="lowerPriorityItem">use null to address any unknown</param>
    ''' <returns></returns>
    Public Function TryDeclarePreference(higherPriorityItem As T, lowerPriorityItem As T) As Boolean

      If (higherPriorityItem Is Nothing AndAlso lowerPriorityItem Is Nothing) Then
        Throw New ArgumentNullException("only one of the given args can be null!")
      End If
      If (higherPriorityItem IsNot Nothing AndAlso lowerPriorityItem IsNot Nothing AndAlso ReferenceEquals(higherPriorityItem, lowerPriorityItem)) Then
        Throw New ArgumentNullException("the given args cannot be equal")
      End If

      SyncLock _Preferences

        SyncLock _ItemsByPriority
          If (Not _ItemsByPriority.Contains(higherPriorityItem)) Then
            Dim indexOfNull = _ItemsByPriority.IndexOf(Nothing)
            _ItemsByPriority.Insert(indexOfNull, higherPriorityItem)
          End If

          If (Not _ItemsByPriority.Contains(lowerPriorityItem)) Then
            Dim indexOfNull = _ItemsByPriority.IndexOf(Nothing)
            _ItemsByPriority.Insert(indexOfNull, lowerPriorityItem)
          End If
        End SyncLock

        Dim newPreference As New Tuple(Of T, T)(higherPriorityItem, lowerPriorityItem)
        Dim extendedPReferences As IEnumerable(Of Tuple(Of T, T)) = _Preferences.Union({newPreference})

        If (Me.TrySort(extendedPReferences)) Then
          _Preferences.Add(newPreference)
          Return True
        End If

        Return False
      End SyncLock

    End Function

    Private Function TrySort(preferences As IEnumerable(Of Tuple(Of T, T))) As Boolean
      Dim snapshot = _ItemsByPriority.ToList()

      'do the complete sorting multiple times, because
      'later processed rules could corrupt the results
      'from other rules - so we do this as often, as there
      'are rules present +1. if were are not compliant after
      'that, we can be sure, that the rules are cyclic...
      For i As Integer = 1 To preferences.Count + 1

        Dim wasReordered As Boolean = False
        For Each pref In preferences 'move items for every pereference

          Dim indexOfPrefered = snapshot.IndexOf(pref.Item1)
          Dim indexOfOverridden = snapshot.IndexOf(pref.Item2)

          If (indexOfPrefered > indexOfOverridden) Then
            snapshot.RemoveAt(indexOfPrefered)
            snapshot.Insert(indexOfOverridden, pref.Item1)
            wasReordered = True
          End If

        Next

        If (Not wasReordered) Then
          _ItemsByPriority = snapshot
          Return True
        End If

      Next

      Return False 'cyclic reference detected
    End Function

    Public Function PriorityOf(item As T) As Integer
      SyncLock _ItemsByPriority
        Dim indexOfNull = _ItemsByPriority.IndexOf(Nothing)
        Dim idx = _ItemsByPriority.IndexOf(item)
        If (idx = -1) Then
          Return indexOfNull
        Else
          Return idx
        End If
      End SyncLock
    End Function

#Region " Sort "

    Public Function SortByPriority(input As IEnumerable(Of T)) As IOrderedEnumerable(Of T)
      SyncLock _ItemsByPriority
        Dim indexOfNull = _ItemsByPriority.IndexOf(Nothing)
        Return input.OrderBy(
        Function(e)
          Dim idx = _ItemsByPriority.IndexOf(e)
          If (idx = -1) Then
            Return indexOfNull
          Else
            Return idx
          End If
        End Function
       )
      End SyncLock
    End Function

    Public Function SortByPriorityDesc(input As IEnumerable(Of T)) As IOrderedEnumerable(Of T)
      SyncLock _ItemsByPriority
        Dim indexOfNull = _ItemsByPriority.IndexOf(Nothing)
        Return input.OrderByDescending(
        Function(e)
          Dim idx = _ItemsByPriority.IndexOf(e)
          If (idx = -1) Then
            Return indexOfNull
          Else
            Return idx
          End If
        End Function
       )
      End SyncLock
    End Function

#End Region

  End Class

End Namespace
