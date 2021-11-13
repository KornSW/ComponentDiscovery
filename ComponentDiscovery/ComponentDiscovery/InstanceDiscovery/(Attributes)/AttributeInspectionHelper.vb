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

Namespace Composition.InstanceDiscovery

  <DebuggerDisplay("AttributeInspectionHelper: {TargetType}")>
  Friend Class AttributeInspectionHelper

    Private Shared _Instances As New Dictionary(Of Type, AttributeInspectionHelper)

    Public Shared Function GetInstanceOf(t As Type) As AttributeInspectionHelper
      SyncLock _Instances
        Dim instance As AttributeInspectionHelper = Nothing
        If (Not _Instances.TryGetValue(t, instance)) Then
          instance = New AttributeInspectionHelper(t)
          _Instances.Add(t, instance)
        End If
        Return instance
      End SyncLock
    End Function

    Private _TargetType As Type

    Public ReadOnly Property TargetType As Type
      Get
        Return _TargetType
      End Get
    End Property

    Private Sub New(targetType As Type)
      _TargetType = targetType
    End Sub

#Region " Priorization Rules "

    Private _PriorizationRules As List(Of Tuple(Of Type, Boolean)) = Nothing

    Public ReadOnly Property PriorizationRules As Tuple(Of Type, Boolean)()
      Get
        If (_PriorizationRules Is Nothing) Then
          _PriorizationRules = New List(Of Tuple(Of Type, Boolean))

          Dim classAttribs = _TargetType.GetCustomAttributes(False)
          For Each a In classAttribs.OfType(Of PriorizeInstanceDiscoveryAgainstAttribute)
            _PriorizationRules.Add(New Tuple(Of Type, Boolean)(a.ForeignOriginType, a.CurrentIsPreferred))
          Next
          For Each a In classAttribs.OfType(Of PriorizeInstanceDiscoveryAttribute)
            _PriorizationRules.Add(New Tuple(Of Type, Boolean)(Nothing, a.CurrentIsPreferred))
          Next

        End If
        Return _PriorizationRules.ToArray()
      End Get
    End Property

#End Region

#Region " Provider Entries "

    Private _InstanceGetters As List(Of InstanceGetter) = Nothing

    Public ReadOnly Property InstanceGetters As InstanceGetter()
      Get
        If (_InstanceGetters Is Nothing) Then
          _InstanceGetters = New List(Of InstanceGetter)()
          Me.BuildInstanceGetters(_InstanceGetters)
        End If
        Return _InstanceGetters.ToArray()
      End Get
    End Property

    Private Shared Function InspectParameterAttributes(parameters As ParameterInfo(), typesToDiscover As List(Of Type), injectionDemands As List(Of InjectionDemand), ByRef errorMessageBuffer As String) As Boolean
      For Each parameter In parameters
        Dim anyInjectionAttributeFound As Boolean = False
        Dim typeToDiscover As Type = Nothing
        Dim injectionDemand As InjectionDemand

        For Each paramAttr In parameter.GetCustomAttributes()
          If (TypeOf (paramAttr) Is InjectAttribute) Then
            If (anyInjectionAttributeFound) Then
              errorMessageBuffer = "There is more than one 'InjectAttribute' or 'TryInjectAttribute' for the same parameter!"
              Return False
            End If
            anyInjectionAttributeFound = True
            typeToDiscover = DirectCast(paramAttr, InjectAttribute).TypeToDiscover
            injectionDemand = InjectionDemand.SuccessOrThrow
          End If
          If (TypeOf (paramAttr) Is TryInjectAttribute) Then
            If (anyInjectionAttributeFound) Then
              errorMessageBuffer = "There is more than one 'InjectAttribute' or 'TryInjectAttribute' for the same parameter!"
              Return False
            End If
            anyInjectionAttributeFound = True
            typeToDiscover = DirectCast(paramAttr, TryInjectAttribute).TypeToDiscover
            If (parameter.IsOptional) Then
              injectionDemand = InjectionDemand.SuccessOrSkip
            Else
              injectionDemand = InjectionDemand.SuccessOrNull
            End If
          End If
        Next

        If (typeToDiscover Is Nothing) Then
          typeToDiscover = parameter.ParameterType
        ElseIf (Not parameter.ParameterType.IsAssignableFrom(typeToDiscover)) Then
          errorMessageBuffer = $"An instance of '{typeToDiscover.FullName}' (as requested by the inject attribute) cannot be casted to the parameter type '{parameter.ParameterType}'!"
          Return False
        End If

        If (Not anyInjectionAttributeFound) Then
          If (parameter.IsOptional) Then
            injectionDemand = InjectionDemand.SkipAlways
          Else
            errorMessageBuffer = "There are non-optional PARAMETERS without 'InjectAttribute' or 'TryInjectAttribute'!"
            Return False
          End If
        End If

        typesToDiscover.Add(typeToDiscover)
        injectionDemands.Add(injectionDemand)
      Next

      Return True
    End Function

    Private Sub BuildInstanceGetters(target As List(Of InstanceGetter))
      Dim errorMessageBuffer As String = String.Empty

      'analyze constructors

      For Each ctor In _TargetType.GetConstructors(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)
        Dim attribs = ctor.GetCustomAttributes()
        For Each a In attribs.OfType(Of CreatesDiscoverableInstanceAttribute)
          Dim providedType = a.DiscoverableAsType

          If (providedType Is Nothing) Then
            providedType = _TargetType
          ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(_TargetType)) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}' because the instances of '{_TargetType.FullName}' cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
            Continue For
          End If

          If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
            Continue For
          End If

          Dim newGetter As New InstanceGetter With {
            .OwnerType = _TargetType,
            .ProvidedType = providedType,
            .ProvidesNull = False,
            .LifetimeResponsibility = LifetimeResponsibility.Delegated,
            .Method = AddressOf ctor.Invoke
          }

          If (Not InspectParameterAttributes(ctor.GetParameters(), newGetter.RequestedTypePerParameter, newGetter.InjectionDemandPerParameter, errorMessageBuffer)) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}': " + errorMessageBuffer)
            Continue For
          End If

          target.Add(newGetter)
        Next
      Next

      'analyze methods

      For Each meth In _TargetType.GetMethods(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static Or BindingFlags.InvokeMethod)
        Dim attribs = meth.GetCustomAttributes()
        For Each a In attribs.OfType(Of CreatesDiscoverableInstanceAttribute)
          Dim providedType = a.DiscoverableAsType

          If (meth.ReturnType = GetType(Void)) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the method returns void!")
            Continue For
          End If

          If (Not meth.IsStatic) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to STATIC methods!")
            Continue For
          End If

          If (meth.GetGenericArguments().Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
            Continue For
          End If

          If (providedType Is Nothing) Then
            providedType = meth.ReturnType
          ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(meth.ReturnType)) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns instances of '{meth.ReturnType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
            Continue For
          End If

          If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
            Continue For
          End If

          Dim newGetter As New InstanceGetter With {
            .OwnerType = _TargetType,
            .ProvidedType = providedType,
            .ProvidesNull = False,
            .LifetimeResponsibility = LifetimeResponsibility.Delegated,
            .Method = Function(p As Object()) meth.Invoke(Nothing, p)
          }

          If (Not InspectParameterAttributes(meth.GetParameters(), newGetter.RequestedTypePerParameter, newGetter.InjectionDemandPerParameter, errorMessageBuffer)) Then
            Trace.TraceError($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}': " + errorMessageBuffer)
            Continue For
          End If

          target.Add(newGetter)
        Next

        For Each a In attribs.OfType(Of ProvidesDiscoverableInstanceAttribute)
          Dim providedType = a.DiscoverableAsType

          If (meth.ReturnType = GetType(Void)) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns void!")
            Continue For
          End If

          If (Not meth.IsStatic) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to STATIC methods/properties!")
            Continue For
          End If

          If (meth.GetGenericArguments().Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
            Continue For
          End If

          If (providedType Is Nothing) Then
            providedType = meth.ReturnType
          ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(meth.ReturnType)) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns instances of '{meth.ReturnType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
            Continue For
          End If

          If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
            Continue For
          End If

          Dim newGetter As New InstanceGetter With {
            .OwnerType = _TargetType,
            .ProvidedType = providedType,
            .ProvidesNull = a.ProvidesNullDiscoverable,
            .LifetimeResponsibility = LifetimeResponsibility.Managed,
            .Method = Function(p As Object()) meth.Invoke(Nothing, p)
          }

          If (Not InspectParameterAttributes(meth.GetParameters(), newGetter.RequestedTypePerParameter, newGetter.InjectionDemandPerParameter, errorMessageBuffer)) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}': " + errorMessageBuffer)
            Continue For
          End If

          target.Add(newGetter)
        Next
      Next

      'analyze properties

      For Each prop In _TargetType.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static)
        Dim attribs = prop.GetCustomAttributes()
        For Each a In attribs.OfType(Of ProvidesDiscoverableInstanceAttribute)
          Dim providedType = a.DiscoverableAsType

          If (Not prop.CanRead) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property is not write-only!")
            Continue For
          End If

          If (Not prop.GetGetMethod().IsStatic) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to STATIC methods/properties!")
            Continue For
          End If

          If (providedType Is Nothing) Then
            providedType = prop.PropertyType
          ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(prop.PropertyType)) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property provides instances of '{prop.PropertyType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
            Continue For
          End If

          If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
            Continue For
          End If

          Dim newGetter As New InstanceGetter With {
            .OwnerType = _TargetType,
            .ProvidedType = providedType,
            .ProvidesNull = a.ProvidesNullDiscoverable,
            .LifetimeResponsibility = LifetimeResponsibility.Managed,
            .Method = Function(p As Object()) prop.GetValue(Nothing, p)
          }

          If (Not InspectParameterAttributes(prop.GetIndexParameters(), newGetter.RequestedTypePerParameter, newGetter.InjectionDemandPerParameter, errorMessageBuffer)) Then
            Trace.TraceError($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}': " + errorMessageBuffer)
            Continue For
          End If

          target.Add(newGetter)
        Next

      Next
    End Sub

    Public Class InstanceGetter
      Public Property OwnerType As Type
      Public Property ProvidedType As Type
      Public Property LifetimeResponsibility As LifetimeResponsibility

      Public Property RequestedTypePerParameter As New List(Of Type)
      Public Property InjectionDemandPerParameter As New List(Of InjectionDemand)

      Public Property Method As Func(Of Object(), Object)
      Public Property ProvidesNull As Boolean
    End Class

#End Region

#Region " Consumer Entries "

    Private _ConsumerEntries As List(Of Tuple(Of Type, Action(Of Object, Object), InjectionDemand)) = Nothing

    Public ReadOnly Property ConsumerEntries As Tuple(Of Type, Action(Of Object, Object), InjectionDemand)()
      Get

        If (_ConsumerEntries Is Nothing) Then
          _ConsumerEntries = New List(Of Tuple(Of Type, Action(Of Object, Object), InjectionDemand))

          For Each meth In _TargetType.GetMethods(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Instance Or BindingFlags.InvokeMethod)
            Dim attribs = meth.GetCustomAttributes()

            For Each a In attribs.OfType(Of InjectAttribute)
              Dim params = meth.GetParameters()

              If (meth.IsStatic) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
                Continue For
              End If

              If (meth.GetGenericArguments().Any()) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
                Continue For
              End If

              If (params.Length = 0) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has no parameter!")
                Continue For
              End If

              If (params.Length > 1) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has more than one parameter (this is currently not supported)!")
                Continue For
              End If

              Dim requestedType As Type
              If (a.TypeToDiscover Is Nothing) Then
                requestedType = params(0).ParameterType
              ElseIf (Not params(0).ParameterType.IsAssignableFrom(a.TypeToDiscover)) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because '{a.TypeToDiscover}' cant be casted to '{params(0).ParameterType}'!")
                Continue For
              Else
                requestedType = a.TypeToDiscover
              End If

              _ConsumerEntries.Add(
                New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                  requestedType,
                  Sub(o, p) meth.Invoke(o, {p}),
                  InjectionDemand.SuccessOrThrow
                )
              )

            Next

            For Each a In attribs.OfType(Of TryInjectAttribute)
              Dim params = meth.GetParameters()

              If (meth.IsStatic) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
                Continue For
              End If

              If (meth.GetGenericArguments().Any()) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
                Continue For
              End If

              If (params.Length = 0) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has no parameter!")
                Continue For
              End If

              If (params.Length > 1) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has more than one parameter (this is currently not supported)!")
                Continue For
              End If

              Dim requestedType As Type
              If (a.TypeToDiscover Is Nothing) Then
                requestedType = params(0).ParameterType
              ElseIf (Not params(0).ParameterType.IsAssignableFrom(a.TypeToDiscover)) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Method '{_TargetType.FullName}.{meth.Name}' because '{a.TypeToDiscover}' cant be casted to '{params(0).ParameterType}'!")
                Continue For
              Else
                requestedType = a.TypeToDiscover
              End If

              _ConsumerEntries.Add(
                New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                  requestedType,
                  Sub(o, p) meth.Invoke(o, {p}),
                  InjectionDemand.SuccessOrSkip
                )
              )

            Next

          Next

          For Each prop In _TargetType.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Instance)
            Dim attribs = prop.GetCustomAttributes()

            For Each a In attribs.OfType(Of InjectAttribute)

              If (Not prop.CanWrite) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property is not read-only!")
                Continue For
              End If

              If (prop.GetGetMethod().IsStatic) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
                Continue For
              End If

              If (prop.GetIndexParameters().Any()) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to PARAMETERLESS methods/properties!")
                Continue For
              End If

              Dim requestedType As Type
              If (a.TypeToDiscover Is Nothing) Then
                requestedType = prop.PropertyType
              ElseIf (Not prop.PropertyType.IsAssignableFrom(a.TypeToDiscover)) Then
                Trace.TraceError($"Instance-Discovery will ignore the InjectAttribute on Property '{_TargetType.FullName}.{prop.Name}'  because '{a.TypeToDiscover}' cant be casted to '{ prop.PropertyType}'!")
                Continue For
              Else
                requestedType = a.TypeToDiscover
              End If

              _ConsumerEntries.Add(
                New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                  requestedType,
                  Sub(o, v) prop.SetValue(o, v),
                  InjectionDemand.SuccessOrThrow
                )
              )

            Next

            For Each a In attribs.OfType(Of TryInjectAttribute)

              If (Not prop.CanWrite) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property is not read-only!")
                Continue For
              End If

              If (prop.GetGetMethod().IsStatic) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
                Continue For
              End If

              If (prop.GetIndexParameters().Any()) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to PARAMETERLESS methods/properties!")
                Continue For
              End If

              Dim requestedType As Type
              If (a.TypeToDiscover Is Nothing) Then
                requestedType = prop.PropertyType
              ElseIf (Not prop.PropertyType.IsAssignableFrom(a.TypeToDiscover)) Then
                Trace.TraceError($"Instance-Discovery will ignore the TryInjectAttribute on Property '{_TargetType.FullName}.{prop.Name}'  because '{a.TypeToDiscover}' cant be casted to '{ prop.PropertyType}'!")
                Continue For
              Else
                requestedType = a.TypeToDiscover
              End If

              _ConsumerEntries.Add(
                New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                  requestedType,
                  Sub(o, v) prop.SetValue(o, v),
                  InjectionDemand.SuccessOrSkip
                )
              )

            Next

          Next

        End If

        Return _ConsumerEntries.ToArray()
      End Get
    End Property

#End Region

  End Class

End Namespace
