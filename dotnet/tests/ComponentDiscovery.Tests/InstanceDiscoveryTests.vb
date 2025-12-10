'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports ComponentDiscovery
Imports ComponentDiscovery.ClassificationApproval
Imports ComponentDiscovery.ClassificationDetection
Imports Composition.InstanceDiscovery
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class InstanceDiscoveryTests
#Region "..."

  <TestInitialize()>
  Public Sub Initialize()


  End Sub

  <TestCleanup()>
  Public Sub Cleanup()

  End Sub

#End Region

  <TestMethod()>
  Public Sub TestInstanceDiscovery1()

    Dim discoveredInstanxe As IInterfaceToDisvocer

    discoveredInstanxe = InstanceDiscoveryContext.Current.GetInstance(Of IInterfaceToDisvocer)(False)

    Assert.IsNotNull(discoveredInstanxe)

  End Sub

End Class

Public Interface IInterfaceToDisvocer

End Interface

<SupportsInstanceDiscovery>
Public Class ClassToDiscover
  Implements IInterfaceToDisvocer

  Private Shared _Singleton As New ClassToDiscover()

  <ProvidesDiscoverableInstance(GetType(IInterfaceToDisvocer))>
  Public Shared ReadOnly Property Current As ClassToDiscover
    Get
      Return _Singleton
    End Get
  End Property

End Class
