Imports System

<AttributeUsage(AttributeTargets.Assembly, AllowMultiple:=True)>
Public Class TypesClassificationAttribute
  Inherits Attribute

  Private _NamespaceAndTypenameMask As String
  Private _DimensionName As String
  Private _ClassificationExpression As String

  ''' <param name="namespaceAndTypenameMask">a include-mask for namespace and/or typename (can be used with wildcard '*')</param>
  ''' <param name="dimensionName"></param>
  ''' <param name="classificationExpression"></param>
  Public Sub New(namespaceAndTypenameMask As String, dimensionName As String, classificationExpression As String)
    MyBase.New()

    _NamespaceAndTypenameMask = namespaceAndTypenameMask
    _DimensionName = dimensionName
    _ClassificationExpression = classificationExpression

  End Sub

  Public ReadOnly Property NamespaceAndTypenameMask As String
    Get
      Return _NamespaceAndTypenameMask
    End Get
  End Property

  Public ReadOnly Property DimensionName As String
    Get
      Return _DimensionName
    End Get
  End Property

  Public ReadOnly Property ClassificationExpression As String
    Get
      Return _ClassificationExpression
    End Get
  End Property

End Class
