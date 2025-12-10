'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Ambience
Imports System.Collections.Generic
Imports System.Diagnostics
Imports ComponentDiscovery

Namespace Composition.InstanceDiscovery

  Partial Class InstanceDiscoveryContext
    Implements IDisposable

    'in order to avoid using any big external ambience/scope-management framework,  
    'we provide a simple build-in implementation to manage our scoped singletons internally...

    Private Shared _BindingDiscriminatorGetter As Func(Of String) = Function() "(global)"

    Public Shared Sub ActivateScoping(bindingDiscriminatorGetter As Func(Of String))
      _BindingDiscriminatorGetter = bindingDiscriminatorGetter
      DisposeInternalManagedSingletons()
    End Sub

    Private Shared _SelfManagedAssemblyIndexerInstances As New Dictionary(Of String, IAssemblyIndexer)
    Private Shared _SelfManagedTypeIndexerInstances As New Dictionary(Of String, ITypeIndexer)
    Private Shared _SelfManagedDiscoveryContextInstances As New Dictionary(Of String, InstanceDiscoveryContext)
    Private Shared _SelfManagedAttributeBasedProviderInstances As New Dictionary(Of String, ProviderRepository(Of IDiscoverableInstanceProvider))

    Private Shared _CurrentAssemblyIndexerGetter As Func(Of IAssemblyIndexer) = (
      Function()
        Dim bindingDiscriminator As String = _BindingDiscriminatorGetter.Invoke()
        SyncLock (_SelfManagedAssemblyIndexerInstances)
          Dim currentInstance As IAssemblyIndexer = Nothing
          If (Not _SelfManagedAssemblyIndexerInstances.TryGetValue(bindingDiscriminator, currentInstance)) Then
            currentInstance = New AssemblyIndexer(
              enableResolvePathsBinding:=True, enableAppDomainBinding:=True, preferAssemblyLoadingViaFusion:=False
            )
            _SelfManagedAssemblyIndexerInstances.Add(bindingDiscriminator, currentInstance)
          End If
          Return currentInstance
        End SyncLock
      End Function
    )

    Private Shared _CurrentTypeIndexerGetter As Func(Of ITypeIndexer) = (
      Function()
        Dim bindingDiscriminator As String = _BindingDiscriminatorGetter.Invoke()
        SyncLock (_SelfManagedTypeIndexerInstances)
          Dim currentInstance As ITypeIndexer = Nothing
          If (Not _SelfManagedTypeIndexerInstances.TryGetValue(bindingDiscriminator, currentInstance)) Then
            Dim assemblyIndexerToBind As IAssemblyIndexer = _CurrentAssemblyIndexerGetter.Invoke()
            currentInstance = New TypeIndexer(assemblyIndexerToBind)
            _SelfManagedTypeIndexerInstances.Add(bindingDiscriminator, currentInstance)
          End If
          Return currentInstance
        End SyncLock
      End Function
    )

    Public Shared Sub UseExternalManagedTypeIndexer(typeIndexerGetter As Func(Of ITypeIndexer))
      _CurrentTypeIndexerGetter = typeIndexerGetter
      DisposeInternalManagedSingletons()
      _CurrentAssemblyIndexerGetter =
        Function()
          Throw New Exception("When using an externally managed TypeIndexer, the AssemblyIndexer cannot be accessed internally anymore.")
        End Function
    End Sub

    Private Shared _CurrentProviderGetter As Func(Of IEnumerable(Of IDiscoverableInstanceProvider)) = (
      Function()

        Dim bindingDiscriminator As String = _BindingDiscriminatorGetter.Invoke()
        Dim currentInstance As ProviderRepository(Of IDiscoverableInstanceProvider) = Nothing
        Dim typeIndexerToBind As ITypeIndexer = Nothing

        SyncLock (_SelfManagedAttributeBasedProviderInstances)
          If (Not _SelfManagedAttributeBasedProviderInstances.TryGetValue(bindingDiscriminator, currentInstance)) Then
            typeIndexerToBind = _CurrentTypeIndexerGetter.Invoke()
            currentInstance = New ProviderRepository(Of IDiscoverableInstanceProvider)(typeIndexerToBind)
            _SelfManagedAttributeBasedProviderInstances.Add(bindingDiscriminator, currentInstance)
          End If
        End SyncLock

        'were leaving the SyncLock before subscribing the typeindexer to avoid deadlocks.
        'Because we could implicit trigger a recursion to this method again - in this case we want to return
        'the already created instance, because the subscription will also work retroactively.

        'if we have created a New instance, we have to bind it to the type indexer
        If (typeIndexerToBind IsNot Nothing) Then
          'instances of this special-provider will need to be added manually, because there is no parameterless constructor 
          typeIndexerToBind.SubscribeForApplicableTypeFound(Of SupportsInstanceDiscoveryAttribute)(
            False,
            Sub(t)
              Dim newProvider = AttributeBasedDiscoverableInstanceProvider.CreateForCandidateIfValid(t)
              If (newProvider IsNot Nothing) Then
                currentInstance.AddProvider(newProvider)
              End If
            End Sub
          )
        End If

        Return currentInstance.Providers
      End Function
    )

    Private Shared _CurrentContextGetter As CurrentContextGetter(Of InstanceDiscoveryContext) = (
      Function()
        Dim bindingDiscriminator As String = _BindingDiscriminatorGetter.Invoke()
        SyncLock (_SelfManagedDiscoveryContextInstances)
          Dim currentInstance As InstanceDiscoveryContext = Nothing
          If (Not _SelfManagedDiscoveryContextInstances.TryGetValue(bindingDiscriminator, currentInstance)) Then
            currentInstance = New InstanceDiscoveryContext(bindingDiscriminator)
            _SelfManagedDiscoveryContextInstances.Add(bindingDiscriminator, currentInstance)
          End If
          Return currentInstance
        End SyncLock
      End Function
    )

    Public Shared ReadOnly Property Current As InstanceDiscoveryContext
      Get
        Return _CurrentContextGetter.Invoke()
      End Get
    End Property

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ContextCreationHandler As ContextCreationHandler(Of InstanceDiscoveryContext) = (
      Sub(context As InstanceDiscoveryContext)
      End Sub
    )

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ContextDisposalHandler As ContextDisposalHandler(Of InstanceDiscoveryContext) = (
      Sub(context As InstanceDiscoveryContext)
      End Sub
    )

    <Obsolete("You should use 'ActivateScoping' and/or 'UseExternalManagedTypeIndexer' instead...")>
    Public Shared Sub HookAmbienceManagment(
      currentContextGetter As CurrentContextGetter(Of InstanceDiscoveryContext),
      contextCreationHandler As ContextCreationHandler(Of InstanceDiscoveryContext),
      contextDisposalHandler As ContextDisposalHandler(Of InstanceDiscoveryContext)
    )
      _CurrentContextGetter = currentContextGetter
      _ContextCreationHandler = contextCreationHandler
      _ContextDisposalHandler = contextDisposalHandler

      DisposeInternalManagedSingletons()
    End Sub

    <Obsolete("You should use 'ActivateScoping' and/or 'UseExternalManagedTypeIndexer' instead...")>
    Public Shared Sub HookProviderDiscovery(providerDiscoveryMethod As Func(Of IEnumerable(Of IDiscoverableInstanceProvider)))
      _CurrentProviderGetter = providerDiscoveryMethod
      _ProviderDiscoveryMethodIsHooked = True
      DisposeInternalManagedSingletons()
    End Sub

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ProviderDiscoveryMethodIsHooked As Boolean = False

    Friend Shared Sub DisposeInternalManagedSingletons()

      SyncLock (_SelfManagedDiscoveryContextInstances)

        For Each instance In _SelfManagedDiscoveryContextInstances.Values
          Try
            instance.Dispose()
          Catch
          End Try
        Next

        _SelfManagedDiscoveryContextInstances.Clear()
      End SyncLock

      SyncLock (_SelfManagedTypeIndexerInstances)

        For Each instance In _SelfManagedTypeIndexerInstances.Values
          Try
            instance.Dispose()
          Catch
          End Try
        Next

        _SelfManagedTypeIndexerInstances.Clear()
      End SyncLock

      SyncLock (_SelfManagedAssemblyIndexerInstances)

        For Each instance In _SelfManagedAssemblyIndexerInstances.Values
          Try
            instance.Dispose()
          Catch
          End Try
        Next

        _SelfManagedAssemblyIndexerInstances.Clear()
      End SyncLock

    End Sub

  End Class

End Namespace
