'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace Composition.InstanceDiscovery

  Friend Class InjectionHelper

    Private Sub New()
    End Sub

    Public Shared Function CreateInstanceArrayForParameterInjection(requestedTypePerParameter As IEnumerable(Of Type), injectionDemandPerParameter As IEnumerable(Of InjectionDemand), ownerType As Type, sourceContext As InstanceDiscoveryContext) As Object()

      If (requestedTypePerParameter Is Nothing) Then
        Return {}
      End If

      Dim instances As New List(Of Object)()

      For i As Integer = 0 To (requestedTypePerParameter.Count - 1)
        Dim requestedType As Type = requestedTypePerParameter(i)
        Dim injectionDemand As InjectionDemand = injectionDemandPerParameter(i)

        If (injectionDemand = InjectionDemand.SkipAlways) Then
          'the magic-value of .NET for optional parameters which should not be passed!
          instances.Add(Type.Missing)
        Else

          Dim instance As Object = Nothing
          Dim success As Boolean = False
          If (GetType(InstanceDiscoveryContext).IsAssignableFrom(requestedType) AndAlso requestedType.IsAssignableFrom(sourceContext.GetType())) Then
            instance = sourceContext
            success = True
          Else
            success = sourceContext.TryGetInstanceOf(requestedType, instance)
          End If

          If (success) Then
            instances.Add(instance)
          ElseIf (injectionDemand = InjectionDemand.SuccessOrNull) Then
            instances.Add(Nothing)
          ElseIf (injectionDemand = InjectionDemand.SuccessOrSkip) Then
            'the magic-value of .NET for optional parameters which should not be passed!
            instances.Add(Type.Missing)
          Else '(injectionDemand.SuccessOrThrow)
            Throw New Exception($"InstanceDiscovery cannot fulfill injection demand for requested type '{requestedType.FullName}' (in '{ownerType.FullName}') because no provider can supply an instance of this type!")
          End If

        End If

      Next

      Return instances.ToArray()
    End Function

    Public Shared Sub InjectInstacesIntoMembersOf(consumer As Object, sourceContext As InstanceDiscoveryContext)
      Dim consumerType As Type = consumer.GetType()
      Dim accessor = AttributeInspectionHelper.GetInstanceOf(consumerType)

      For Each entry In accessor.ConsumerEntries

        Dim requestedType As Type = entry.Item1
        Dim injectionDemand As InjectionDemand = entry.Item3
        Dim injectorMethod As Action(Of Object, Object) = entry.Item2

        Dim instance As Object = Nothing
        Dim success As Boolean = False
        If (GetType(InstanceDiscoveryContext).IsAssignableFrom(requestedType) AndAlso requestedType.IsAssignableFrom(sourceContext.GetType())) Then
          instance = sourceContext
          success = True
        Else
          success = sourceContext.TryGetInstanceOf(requestedType, instance)
        End If

        If (success) Then
          injectorMethod.Invoke(consumer, instance)
        ElseIf (injectionDemand = InjectionDemand.SuccessOrNull) Then
          injectorMethod.Invoke(consumer, Nothing)
        ElseIf (injectionDemand = InjectionDemand.SuccessOrSkip) Then
          'just do nothing
        Else '(injectionDemand.SuccessOrThrow)
          Throw New Exception($"InstanceDiscovery cannot fulfill injection demand for requested type '{requestedType.FullName}' (in '{consumerType.FullName}') because no provider can supply an instance of this type!")
        End If

      Next

    End Sub

  End Class

End Namespace
