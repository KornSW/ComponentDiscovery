Imports System
Imports System.Collections.Generic

Friend Class InjectionHelper

  Private Sub New()
  End Sub

  ''' <summary>
  ''' 
  ''' </summary>
  ''' <param name="parameterTypes"></param>
  ''' <param name="sourceContext"></param>
  ''' <param name="returnNullOnMissingProvider">
  ''' Requires, that the discovery for each type needs to be vaild
  ''' (this means that a instance still can be null, but only as offically returned result)
  ''' If not, null will be returned instead of an array!
  ''' </param>
  ''' <returns></returns>
  Public Shared Function CreateInstanceArrayForParameterInjection(parameterTypes As Type(), sourceContext As InstanceDiscoveryContext, returnNullOnMissingProvider As Boolean) As Object()

    If (parameterTypes Is Nothing) Then
      Return {}
    End If

    Dim instances As New List(Of Object)()
    For Each parameterType In parameterTypes
      Dim instance As Object = Nothing
      If (sourceContext.TryGetInstanceOf(parameterType, instance) = False AndAlso returnNullOnMissingProvider) Then
        Return Nothing
      End If
      instances.Add(instance)
    Next

    Return instances.ToArray()
  End Function

  Public Shared Sub InjectInstacesIntoMembersOf(consumer As Object, sourceContext As InstanceDiscoveryContext)
    Dim consumerType As Type = consumer.GetType()
    Dim accessor = AttributeBasedAccessor.GetInstanceOf(consumerType)

    For Each entry In accessor.ConsumerEntries
      Dim instance = sourceContext.GetInstanceOf(entry.Item1, False)
      If (instance IsNot Nothing) Then
        'inject
        entry.Item2.Invoke(consumer, instance)
      End If
    Next

  End Sub

End Class
