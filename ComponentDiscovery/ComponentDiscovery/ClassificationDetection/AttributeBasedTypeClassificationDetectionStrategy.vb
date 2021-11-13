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

Namespace ComponentDiscovery.ClassificationDetection

  Public Class AttributeBasedTypeClassificationDetectionStrategy
    Implements ITypeClassificationDetectionStrategy

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _IncludeAttributesOfBaseClasses As Boolean

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private _NamespaceAttributesPerAssembly As New Dictionary(Of Assembly, NamespaceClassificationAttribute())

    Public Sub New()
      MyClass.New(True)
    End Sub

    Public Sub New(includeAttributesOfBaseClasses As Boolean)
      _IncludeAttributesOfBaseClasses = includeAttributesOfBaseClasses
    End Sub

    Public ReadOnly Property IncludeAttributesOfBaseClasses As Boolean
      Get
        Return _IncludeAttributesOfBaseClasses
      End Get
    End Property

    Public Overridable Function TryDetectClassificationsForType(
      t As Type, taxonomicDimensionName As String, ByRef classifications As String()
    ) As Boolean Implements ITypeClassificationDetectionStrategy.TryDetectClassificationsForType

      Dim lDimensionName As String = taxonomicDimensionName.ToLower()
      Dim results As New List(Of String)

      Dim attributesOnType = t.GetCustomAttributes(_IncludeAttributesOfBaseClasses)

      For Each a In attributesOnType.OfType(Of TypeClassificationAttribute)
        If (String.Equals(taxonomicDimensionName, a.TaxonomicDimensionName)) Then
          results.Add(a.ClassificationExpression)
        End If
      Next

      Dim ignoreAttributesOnType = attributesOnType.OfType(Of IgnoreNamespaceClassificationsAttribute)
      If (ignoreAttributesOnType.Where(Function(a) String.Equals(taxonomicDimensionName, a.TaxonomicDimensionName)).Any()) Then
        classifications = results.ToArray()
        Return True
      End If

      For Each a In Me.GetNamespaceAttributesForAssembly(t.Assembly)
        If (String.Equals(taxonomicDimensionName, a.TaxonomicDimensionName)) Then
          If (t.FullName.MatchesWildcardMask(a.NamespaceAndOrTypenameMask, True)) Then
            results.Add(a.ClassificationExpression)
          End If
        End If
      Next

      If (results.Any()) Then
        classifications = results.ToArray()
        Return True
      Else
        Return False
      End If

    End Function

    Protected Function GetNamespaceAttributesForAssembly(ass As Assembly) As NamespaceClassificationAttribute()

      If (_NamespaceAttributesPerAssembly.ContainsKey(ass)) Then
        Return _NamespaceAttributesPerAssembly(ass)
      Else
        Dim result = ass.GetCustomAttributes(_IncludeAttributesOfBaseClasses).OfType(Of NamespaceClassificationAttribute).ToArray()
        _NamespaceAttributesPerAssembly.Add(ass, result)
        Return result
      End If

    End Function

  End Class

End Namespace
