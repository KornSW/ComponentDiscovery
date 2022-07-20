'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports ComponentDiscovery

Namespace Composition.InstanceDiscovery

  Partial Class InstanceDiscoveryContext
    Implements IDisposable

    'this is an interceptor to hook up any logic for scope the availability of providers
    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ProviderDiscoveryMethod As Func(Of IEnumerable(Of IDiscoverableInstanceProvider)) = AddressOf GetOrCreateAppdomainScopedProviderRepository

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _ProviderDiscoveryMethodIsHooked As Boolean = False

#Region " default-logic for simple usecases (global-scoped appdomain usage) "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _AppDomainAssemblyIndexer As AssemblyIndexer = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _AppDomainTypeIndexer As TypeIndexer = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _AppDomainProviderRepo As ProviderRepository(Of IDiscoverableInstanceProvider) = Nothing

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared _AppDomainScopedProviderForAttribs As AttributeBasedDiscoverableInstanceProvider = Nothing

    Private Shared Iterator Function GetOrCreateAppdomainScopedProviderRepository() As IEnumerable(Of IDiscoverableInstanceProvider)

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

      For Each p In _AppDomainProviderRepo.Providers
        Yield p
      Next

    End Function

#End Region

    Public Shared Sub HookProviderDiscovery(providerDiscoveryMethod As Func(Of IEnumerable(Of IDiscoverableInstanceProvider)))
      _ProviderDiscoveryMethod = providerDiscoveryMethod
      _ProviderDiscoveryMethodIsHooked = True
    End Sub

  End Class

End Namespace
