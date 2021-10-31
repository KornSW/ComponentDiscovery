Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection

<DebuggerDisplay("AttributeBasedAccessor: {TargetType}")>
Friend Class AttributeBasedAccessor

  Private Shared _Instances As New Dictionary(Of Type, AttributeBasedAccessor)

  Public Shared Function GetInstanceOf(t As Type) As AttributeBasedAccessor
    SyncLock _Instances
      Dim instance As AttributeBasedAccessor = Nothing
      If (Not _Instances.TryGetValue(t, instance)) Then
        instance = New AttributeBasedAccessor(t)
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

  Private Sub BuildInstanceGetters(target As List(Of InstanceGetter))

    'analyze constructors

    For Each ctor In _TargetType.GetConstructors(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.CreateInstance)
      Dim attribs = ctor.GetCustomAttributes()
      For Each a In attribs.OfType(Of CreatesDiscoverableInstanceAttribute)
        Dim providedType = a.DiscoverableAsType

        If (providedType Is Nothing) Then
          providedType = _TargetType
        ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(_TargetType)) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}' because the instances of '{_TargetType.FullName}' cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
          Continue For
        End If

        If (a.InjectionDemand = InjectionDemand.Disabled AndAlso ctor.GetParameters().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}' because there are PARAMETERS required and the attribute is not declaring that INJECTION should be enabled!")
          Continue For
        End If

        If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Constructor '{_TargetType.FullName}.{ctor.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
          Continue For
        End If

        target.Add(
          New InstanceGetter With {
            .ProvidedType = providedType,
            .ProvidesNull = False,
            .InjectionDemand = a.InjectionDemand,
            .LifetimeResponsibility = LifetimeResponsibility.Delegated,
            .ParameterTypes = ctor.GetParameters().Where(Function(p) Not p.IsOptional).Select(Function(p) p.ParameterType).ToArray(),
            .Method = AddressOf ctor.Invoke
          }
        )

      Next
    Next

    'analyze methods

    For Each meth In _TargetType.GetMethods(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static Or BindingFlags.InvokeMethod)
      Dim attribs = meth.GetCustomAttributes()
      For Each a In attribs.OfType(Of CreatesDiscoverableInstanceAttribute)
        Dim providedType = a.DiscoverableAsType

        If (meth.ReturnType = GetType(Void)) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the method returns void!")
          Continue For
        End If

        If (Not meth.IsStatic) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to STATIC methods!")
          Continue For
        End If

        If (meth.GetGenericArguments().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
          Continue For
        End If

        If (providedType Is Nothing) Then
          providedType = meth.ReturnType
        ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(meth.ReturnType)) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns instances of '{meth.ReturnType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
          Continue For
        End If

        If (a.InjectionDemand = InjectionDemand.Disabled AndAlso meth.GetParameters().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because there are PARAMETERS required and the attribute is not declaring that INJECTION should be enabled!")
          Continue For
        End If

        If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the CreatesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
          Continue For
        End If

        target.Add(
          New InstanceGetter With {
            .ProvidedType = providedType,
            .ProvidesNull = False,
            .InjectionDemand = a.InjectionDemand,
            .LifetimeResponsibility = LifetimeResponsibility.Delegated,
            .ParameterTypes = meth.GetParameters().Where(Function(p) Not p.IsOptional).Select(Function(p) p.ParameterType).ToArray(),
            .Method = Function(p As Object()) meth.Invoke(Nothing, p)
          }
        )

      Next
      For Each a In attribs.OfType(Of ProvidesDiscoverableInstanceAttribute)
        Dim providedType = a.DiscoverableAsType

        If (meth.ReturnType = GetType(Void)) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns void!")
          Continue For
        End If

        If (Not meth.IsStatic) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to STATIC methods/properties!")
          Continue For
        End If

        If (meth.GetGenericArguments().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
          Continue For
        End If

        If (providedType Is Nothing) Then
          providedType = meth.ReturnType
        ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(meth.ReturnType)) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method returns instances of '{meth.ReturnType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
          Continue For
        End If

        If (meth.GetParameters().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to PARAMETERLESS methods/properties!")
          Continue For
        End If

        If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
          Continue For
        End If

        target.Add(
          New InstanceGetter With {
            .ProvidedType = providedType,
            .ProvidesNull = a.ProvidesNullDiscoverable,
            .InjectionDemand = InjectionDemand.Disabled,
            .LifetimeResponsibility = LifetimeResponsibility.Managed,
            .ParameterTypes = {},
            .Method = Function(p As Object()) meth.Invoke(Nothing, p)
          }
        )

      Next
    Next

    'analyze properties

    For Each prop In _TargetType.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Static)
      Dim attribs = prop.GetCustomAttributes()
      For Each a In attribs.OfType(Of ProvidesDiscoverableInstanceAttribute)
        Dim providedType = a.DiscoverableAsType

        If (Not prop.CanRead) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property is not write-only!")
          Continue For
        End If

        If (Not prop.GetGetMethod().IsStatic) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to STATIC methods/properties!")
          Continue For
        End If

        If (prop.GetIndexParameters().Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to PARAMETERLESS methods/properties!")
          Continue For
        End If

        If (providedType Is Nothing) Then
          providedType = prop.PropertyType
        ElseIf (Not a.DiscoverableAsType.IsAssignableFrom(prop.PropertyType)) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property provides instances of '{prop.PropertyType.FullName}' which cannot be casted to '{a.DiscoverableAsType.FullName}' as declared by the attribute!")
          Continue For
        End If

        If (target.Where(Function(g) g.ProvidedType = providedType).Any()) Then
          Debug.Fail($"Instance-Discovery will ignore the ProvidesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because there is already another attribute declaring a source for instances of '{a.DiscoverableAsType}'!")
          Continue For
        End If

        target.Add(
          New InstanceGetter With {
            .ProvidedType = providedType,
            .ProvidesNull = a.ProvidesNullDiscoverable,
            .InjectionDemand = InjectionDemand.Disabled,
            .LifetimeResponsibility = LifetimeResponsibility.Managed,
            .ParameterTypes = {},
            .Method = Function(p As Object()) prop.GetValue(Nothing, p)
          }
        )

      Next
    Next
  End Sub

  Public Class InstanceGetter
    Public Property ProvidedType As Type
    Public Property LifetimeResponsibility As LifetimeResponsibility
    Public Property InjectionDemand As InjectionDemand
    Public Property ParameterTypes As Type()
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
          For Each a In attribs.OfType(Of CreatesDiscoverableInstanceAttribute)
            Dim params = meth.GetParameters()

            If (meth.IsStatic) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
              Continue For
            End If

            If (meth.GetGenericArguments().Any()) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because this attribute can only be applied to NON-GENERIC methods!")
              Continue For
            End If

            If (params.Length = 0) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has no parameter!")
              Continue For
            End If

            If (params.Length > 1) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Method '{_TargetType.FullName}.{meth.Name}' because the Method has more than one parameter (this is currently not supported)!")
              Continue For
            End If

            _ConsumerEntries.Add(
              New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                params(0).ParameterType,
                Sub(o, p) meth.Invoke(o, {p}),
                a.InjectionDemand
              )
            )

          Next
        Next

        For Each prop In _TargetType.GetProperties(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Instance)
          Dim attribs = prop.GetCustomAttributes()
          For Each a In attribs.OfType(Of ConsumesDiscoverableInstanceAttribute)

            If (Not prop.CanWrite) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because the property is not read-only!")
              Continue For
            End If

            If (prop.GetGetMethod().IsStatic) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to NON-STATIC methods/properties!")
              Continue For
            End If

            If (prop.GetIndexParameters().Any()) Then
              Debug.Fail($"Instance-Discovery will ignore the ConsumesDiscoverableInstanceAttribute on Property '{_TargetType.FullName}.{prop.Name}' because this attribute can only be applied to PARAMETERLESS methods/properties!")
              Continue For
            End If

            _ConsumerEntries.Add(
              New Tuple(Of Type, Action(Of Object, Object), InjectionDemand)(
                prop.PropertyType,
                Sub(o, v) prop.SetValue(o, v),
                a.InjectionDemand
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
