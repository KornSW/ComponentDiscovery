Imports System

''' <summary>
''' Declares, that this NON-STATIC METHOD (VOID) or NON-STATIC+PARAMETERLESS PROPERTY publicates the demand to get an discovered instance injected.
''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
''' </summary>
<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property, AllowMultiple:=True)>
Public Class ConsumesDiscoverableInstanceAttribute
  Inherits Attribute

  ''' Declares, that this NON-STATIC METHOD (VOID) or NON-STATIC+PARAMETERLESS PROPERTY publicates the demand to get an discovered instance injected.
  ''' This attribute must be used together with the 'SupportsInstanceDiscoveryAttribute' on the class.
  ''' <param name="injectionDemand"></param>
  Public Sub New(Optional injectionDemand As InjectionDemand = InjectionDemand.Required)
    Me.InjectionDemand = injectionDemand
  End Sub

  Public ReadOnly Property InjectionDemand As InjectionDemand

End Class
