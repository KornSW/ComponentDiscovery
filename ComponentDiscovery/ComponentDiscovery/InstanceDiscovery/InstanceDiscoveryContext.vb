'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports ComponentDiscovery

Namespace Composition.InstanceDiscovery

  ''' <summary>
  ''' provides access to discoverable instances
  ''' </summary>
  Partial Public Class InstanceDiscoveryContext
    Implements IDisposable

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ProvidersUnsorted As IDiscoverableInstanceProvider()

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _ProvidersByPriority As IDiscoverableInstanceProvider() = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _SelfManagedInstances As New Dictionary(Of Type, Object)

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _PriorityRules As New PriorityList(Of Type)

    Public Sub New()

      _ContextCreationHandler.Invoke(Me)

      'create a snapshot, so that the amount of available providers cannot change during the usage of the context
      _ProvidersUnsorted = _ProviderDiscoveryMethod.Invoke()

      'first lets add the overriding rules
      For Each p In _ProvidersUnsorted
        Dim providerOriginType As Type = p.RepresentingOriginType
        p.DeclarePriorizationRules(
          Sub(foreignOrigin As Type, provHasPriority As Boolean)
            Dim higher As Type
            Dim lower As Type
            If (provHasPriority) Then
              higher = providerOriginType
              lower = foreignOrigin
            Else
              higher = foreignOrigin
              lower = providerOriginType
            End If
            If (Not _PriorityRules.TryDeclarePreference(higher, lower)) Then
              Trace.TraceError($"Instance-Discovery source '{higher}' cannot override '{lower}' because of cyclic references!")
            End If
          End Sub
        )
      Next

    End Sub

    Protected ReadOnly Property ProvidersByPriority As IDiscoverableInstanceProvider()
      Get
        If (_ProvidersByPriority Is Nothing) Then
          SyncLock _PriorityRules
            _ProvidersByPriority = _ProvidersUnsorted.OrderBy(
              Function(p) _PriorityRules.PriorityOf(p.RepresentingOriginType)
            ).ToArray()
          End SyncLock
        End If
        Return _ProvidersByPriority
      End Get
    End Property

    Private ReadOnly Property SelfManagedInstances As Dictionary(Of Type, Object)
      Get
        Return _SelfManagedInstances
      End Get
    End Property

    Private ReadOnly Property PriorityRules As PriorityList(Of Type)
      Get
        Return _PriorityRules
      End Get
    End Property

    Public Function DeclareOriginOverride(Of THigherPriorityOrigin, TLowerPriorityOrigin)() As Boolean
      Return Me.DeclareOriginOverride(GetType(THigherPriorityOrigin), GetType(TLowerPriorityOrigin))
    End Function

    Public Function DeclareOriginOverride(higherPriorityOriginType As Type, lowerPriorityOriginType As Type) As Boolean
      SyncLock _PriorityRules

        If (Not _PriorityRules.TryDeclarePreference(higherPriorityOriginType, lowerPriorityOriginType)) Then
          Trace.TraceError($"Instance-Discovery source '{higherPriorityOriginType.FullName}' cannot override '{lowerPriorityOriginType.FullName}' because of cyclic references!")
          Return False
        End If

        _ProvidersByPriority = Nothing 'invalidate!

        Return True
      End SyncLock
    End Function

    Public Function TryGetInstance(Of TRequestedType)(ByRef instance As TRequestedType) As Boolean
      Dim buffer As Object = Nothing
      If (Me.TryGetInstanceOf(GetType(TRequestedType), buffer)) Then
        instance = DirectCast(buffer, TRequestedType)
        Return True
      End If
      Return False
    End Function

    Public Function GetInstance(Of TRequestedType)(Optional throwIfNotFound As Boolean = True) As TRequestedType
      Return DirectCast(Me.GetInstanceOf(GetType(TRequestedType), throwIfNotFound), TRequestedType)
    End Function

    Public Function GetInstanceOf(requestedType As Type, Optional throwIfNotFound As Boolean = True) As Object
      Dim buffer As Object = Nothing
      If (Me.TryGetInstanceOf(requestedType, buffer)) Then
        Return buffer
      End If
      If (throwIfNotFound) Then
        Throw New Exception($"Cannot provide any instance of '{requestedType.FullName}'. None of the following providers could offer one: {String.Join(", ", _ProvidersByPriority.Select(Function(p) "'" + p.GetType().FullName + "'"))}")
      Else
        Return Nothing
      End If
    End Function

    Public Function TryGetInstanceOf(requestedType As Type, ByRef instance As Object) As Boolean
      Dim foundInstance As Object = Nothing

      SyncLock _PriorityRules
        If (_ProvidersByPriority Is Nothing) Then
          _ProvidersByPriority = _ProvidersUnsorted.OrderBy(
            Function(p) _PriorityRules.PriorityOf(p.RepresentingOriginType)
          ).ToArray()
        End If
      End SyncLock

      SyncLock _SelfManagedInstances

        If (_SelfManagedInstances.ContainsKey(requestedType)) Then
          instance = _SelfManagedInstances(requestedType)
          Return True
        End If

        For Each p As IDiscoverableInstanceProvider In _ProvidersByPriority
          Dim dedicatedTypes As Type() = p.DedicatedDiscoverableTypes
          If (dedicatedTypes Is Nothing OrElse dedicatedTypes.Contains(requestedType)) Then
            Dim ltResponsibility As LifetimeResponsibility

            If (p.TryGetInstance(Me, requestedType, foundInstance, ltResponsibility)) Then

              If (ltResponsibility = LifetimeResponsibility.Delegated AndAlso foundInstance IsNot Nothing) Then
                InjectionHelper.InjectInstacesIntoMembersOf(foundInstance, Me)
                _SelfManagedInstances.Add(requestedType, foundInstance)
              End If

              'an instance of null from the a responsible provider is offical supported and will not cause further iteration!
              'if the provider returned 'true', then this is the handle to use!
              instance = foundInstance
              Return True
            End If

          End If
        Next

      End SyncLock

      Return False
    End Function

  End Class

End Namespace
