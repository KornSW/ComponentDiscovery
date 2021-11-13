'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System

Namespace ComponentDiscovery

  <AttributeUsage(AttributeTargets.Assembly, AllowMultiple:=True)>
  Public Class NamespaceClassificationAttribute
    Inherits Attribute

    Private _NamespaceAndOrTypenameMask As String
    Private _TaxonomicDimensionName As String
    Private _ClassificationExpression As String

    ''' <param name="namespaceAndOrTypenameMask">a include-mask for namespace and/or typename (can be used with wildcard '*')</param>
    ''' <param name="taxonomicDimensionName"></param>
    ''' <param name="classificationExpression"></param>
    Public Sub New(namespaceAndOrTypenameMask As String, taxonomicDimensionName As String, classificationExpression As String)
      MyBase.New()

      _NamespaceAndOrTypenameMask = namespaceAndOrTypenameMask
      _TaxonomicDimensionName = taxonomicDimensionName
      _ClassificationExpression = classificationExpression

    End Sub

    Public ReadOnly Property NamespaceAndOrTypenameMask As String
      Get
        Return _NamespaceAndOrTypenameMask
      End Get
    End Property

    Public ReadOnly Property TaxonomicDimensionName As String
      Get
        Return _TaxonomicDimensionName
      End Get
    End Property

    Public ReadOnly Property ClassificationExpression As String
      Get
        Return _ClassificationExpression
      End Get
    End Property

  End Class

End Namespace
