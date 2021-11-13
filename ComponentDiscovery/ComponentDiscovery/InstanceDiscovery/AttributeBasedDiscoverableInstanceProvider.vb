'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace Composition.InstanceDiscovery

  'NOTE: that this provider cannot (and should not) be found via TypeIndexer because it has no parameterless constructor!
  'usually one should inherit from this class and provide the candiddate-types which have to be inspected.
  'The only reason why this is not an abstract class is to make it usable for dynmic initialization when running the
  'simple-default (see InstanceAccessContext) which have to work 'out of the box'
  <DebuggerDisplay("Attributes on {RepresentingOriginType}")>
  Public Class AttributeBasedDiscoverableInstanceProvider
    Implements IDiscoverableInstanceProvider

    Public Shared Function CreateForValidCandidatesIn(ParamArray candidateTypesToInspect As Type()) As AttributeBasedDiscoverableInstanceProvider()
      Dim createdProviders As New List(Of AttributeBasedDiscoverableInstanceProvider)
      For Each candidateTypeToInspect In candidateTypesToInspect
        Dim createdProvider As AttributeBasedDiscoverableInstanceProvider
        createdProvider = AttributeBasedDiscoverableInstanceProvider.CreateForCandidateIfValid(candidateTypeToInspect)
        If (createdProvider IsNot Nothing) Then
          createdProviders.Add(createdProvider)
        End If
      Next
      Return createdProviders.ToArray()
    End Function

    Public Shared Function CreateForCandidateIfValid(candidateTypeToInspect As Type) As AttributeBasedDiscoverableInstanceProvider
      If (candidateTypeToInspect.GetCustomAttributes(False).Where(Function(a) TypeOf (a) Is SupportsInstanceDiscoveryAttribute).Any()) Then
        Return New AttributeBasedDiscoverableInstanceProvider(candidateTypeToInspect)
      End If
      Return Nothing
    End Function

    Private _Accessor As AttributeInspectionHelper

    Public Sub New(originType As Type)
      Me.RepresentingOriginType = originType
      _Accessor = AttributeInspectionHelper.GetInstanceOf(originType)
    End Sub

    ''' <summary>
    ''' If the provider offers a fixed amount of known types, then it can declare them over this
    ''' property to avoid being asked by the framework for instances of any other type.
    ''' An empty array as return value has the semantic, that no instances of any type is provided.
    ''' An return value of null has the semantic, that the provider has a dynamic amount of known types,
    ''' which forces the framework to ask it each time. 
    ''' </summary>
    Public ReadOnly Property DedicatedDiscoverableTypes As Type() Implements IDiscoverableInstanceProvider.DedicatedDiscoverableTypes
      Get
        Return _Accessor.InstanceGetters.Select(Function(g) g.ProvidedType).ToArray()
      End Get
    End Property

    ''' <summary>
    ''' This used to address an instance-source when relating multiple sources in order to specify
    ''' overriding rules like 'prefer MyCustomizedServiceProvider instead of MyCommonServiceProvider'.
    ''' In most cases this is equal to the concrete implmentation of the 'IDiscoverableInstanceProvider'
    ''' interface except in constellations, where providers are just wrapping another source -> then its
    ''' possible to disclose the 'real' source here (to be addressed by overriding rules)
    ''' </summary>
    Public ReadOnly Property RepresentingOriginType As Type Implements IDiscoverableInstanceProvider.RepresentingOriginType

    ''' <summary>
    ''' Each provider which is added to the InstanceAccessContext be be asked to 
    ''' declare priorization rules (in addition to the possibility to declare priorization
    ''' rules in a centralized way on directly at the InstanceAccessContext). If this
    ''' method is invoked, then the provider can use the callback to create one or more
    ''' rules by passign a foreign/related origin type and a value of 'true' 
    ''' when the current provider shloud be preferred against this type or 'false' when the foreign
    ''' one should be preferred.
    ''' </summary>
    Public Sub DeclarePriorizationRules(callback As Action(Of Type, Boolean)) Implements IDiscoverableInstanceProvider.DeclarePriorizationRules
      For Each rule In _Accessor.PriorizationRules
        callback.Invoke(rule.Item1, rule.Item2)
      Next
    End Sub

    ''' <summary>
    ''' </summary>
    ''' <param name="requestingContext">will be used as source for other instances, if injection is enabled</param>
    ''' <param name="requestedType"></param>
    ''' <param name="instance"></param>
    ''' <param name="lifetimeResponsibility">
    ''' informs, which party (the provider or the consumer) has the responsibility
    ''' to manage the lifecycle of the returned instances including their disposal.
    ''' </param>
    ''' <returns></returns>
    Public Function TryGetInstance(requestingContext As InstanceDiscoveryContext, requestedType As Type, ByRef instance As Object, ByRef lifetimeResponsibility As LifetimeResponsibility) As Boolean Implements IDiscoverableInstanceProvider.TryGetInstance

      Dim getter = _Accessor.InstanceGetters.Where(Function(g) g.ProvidedType = requestedType).FirstOrDefault()

      'we cannot offer this type
      If (getter Is Nothing) Then
        Return False
      End If

      Dim args = InjectionHelper.CreateInstanceArrayForParameterInjection(getter.RequestedTypePerParameter, getter.InjectionDemandPerParameter, getter.OwnerType, requestingContext)

      If (args Is Nothing) Then
        'InjectionDemand was 'Required', but not all instances could be provided...
        Trace.TraceError($"Cannot request instance of '{requestedType.FullName}' from '{Me.RepresentingOriginType.FullName}' because the injection demand could not be met!")
        Return False
      End If

      'access the source!!!
      Dim inst = getter.Method.Invoke(args)

      If (inst Is Nothing AndAlso getter.ProvidesNull = False) Then
        Return False
      End If

      instance = inst
      lifetimeResponsibility = getter.LifetimeResponsibility

      'we can offer this type (regardless, if it is null)
      Return True
    End Function

  End Class

End Namespace
