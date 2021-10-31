Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

''' <summary>
''' provides access to discoverable instances
''' </summary>
Public Class InstanceDiscoveryContext
  Implements IDisposable

  'this is an interceptor to hook up any logic for scope the availability of providers
  Public Shared ProviderRepositoryLookupMethod As Func(Of ProviderRepository(Of IDiscoverableInstanceProvider)) = AddressOf GetOrCreateAppdomainScopedProviderRepository

#Region " default-logic for simple usecases (global-scoped appdomain usage) "

  Private Shared _AppDomainAssemblyIndexer As IAssemblyIndexer = Nothing
  Private Shared _AppDomainTypeIndexer As ITypeIndexer = Nothing
  Private Shared _AppDomainProviderRepo As ProviderRepository(Of IDiscoverableInstanceProvider) = Nothing
  Private Shared _AppDomainScopedProviderForAttribs As AttributeBasedDiscoverableInstanceProvider = Nothing
  Private Shared Function GetOrCreateAppdomainScopedProviderRepository() As ProviderRepository(Of IDiscoverableInstanceProvider)

    If (_AppDomainAssemblyIndexer Is Nothing) Then
      _AppDomainAssemblyIndexer = New AssemblyIndexer() With {.AppDomainBindingEnabled = True}
    End If

    If (_AppDomainTypeIndexer Is Nothing) Then
      _AppDomainTypeIndexer = New TypeIndexer(_AppDomainAssemblyIndexer)
    End If

    If (_AppDomainProviderRepo Is Nothing) Then
      'here the typeindexer will be used to find all 'IDiscoverableInstanceProvider' implementations
      _AppDomainProviderRepo = New ProviderRepository(Of IDiscoverableInstanceProvider)(_AppDomainTypeIndexer)

      'instances of this special-provider will need to be added manually, because there is no parameterless constructor 
      _AppDomainTypeIndexer.SubscribeForApplicableTypeFound(Of SupportsInstanceDiscoveryAttribute)(
        False,
        Sub(t)
          Dim newProvider = AttributeBasedDiscoverableInstanceProvider.CreateForCandidateIfValid(t)
          If (newProvider IsNot Nothing) Then
            _AppDomainProviderRepo.AddProvider(newProvider)
          End If
        End Sub
       )

    End If

    Return _AppDomainProviderRepo
  End Function

#End Region

  Private _ProvidersUnsorted As IDiscoverableInstanceProvider()
  Private _ProvidersByPriority As IDiscoverableInstanceProvider() = Nothing
  Private _SelfManagedInstances As New Dictionary(Of Type, Object)
  Private _OriginsByPriority As New PriorityList(Of Type)

  Public Sub New()

    'create a snapshot, so that the amount of available providers cannot change during the usage of the context
    _ProvidersUnsorted = ProviderRepositoryLookupMethod.Invoke().Providers.ToArray()

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
          If (Not _OriginsByPriority.TryDeclarePreference(higher, lower)) Then
            Debug.Fail($"Instance-Discovery source '{higher}' cannot override '{lower}' because of cyclic references!")
          End If
        End Sub
      )
    Next

  End Sub

  Public Function DeclareOriginOverride(Of THigherPriorityOrigin, TLowerPriorityOrigin)() As Boolean
    Return Me.DeclareOriginOverride(GetType(THigherPriorityOrigin), GetType(TLowerPriorityOrigin))
  End Function

  Public Function DeclareOriginOverride(higherPriorityOriginType As Type, lowerPriorityOriginType As Type) As Boolean
    SyncLock _OriginsByPriority

      If (Not _OriginsByPriority.TryDeclarePreference(higherPriorityOriginType, lowerPriorityOriginType)) Then
        Debug.Fail($"Instance-Discovery source '{higherPriorityOriginType.FullName}' cannot override '{lowerPriorityOriginType.FullName}' because of cyclic references!")
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

    SyncLock _OriginsByPriority
      If (_ProvidersByPriority Is Nothing) Then
        _ProvidersByPriority = _ProvidersByPriority.OrderBy(
          Function(p) _OriginsByPriority.PriorityOf(p.RepresentingOriginType)
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

  ''' <summary>
  ''' disposes all 'self-managed' instances for which the lifetime-handling has been delegated to this context
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    SyncLock _SelfManagedInstances
      For Each selfManagedInstance In _SelfManagedInstances.Values
        If (selfManagedInstance IsNot Nothing AndAlso TypeOf (selfManagedInstance) Is IDisposable) Then
          DirectCast(selfManagedInstance, IDisposable).Dispose()
        End If
      Next
      _SelfManagedInstances.Clear()
    End SyncLock
  End Sub

End Class
