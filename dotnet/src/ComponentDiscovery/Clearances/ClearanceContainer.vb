'Imports System
'Imports System.Collections
'Imports System.Collections.Generic
'Imports ComponentDiscovery

'Namespace ComponentDiscovery

'<Serializable>
'Public Class ClearanceContainer
'  Implements IEnumerable(Of Tuple(Of String, String))

'  Public Sub New()
'  End Sub

'#Region " Full State "

'  ''' <summary>
'  ''' BEFORE!!!
'  ''' </summary>
'  Public Event SettingRawState(sender As ClearanceContainer, newRawState As Byte())

'  ''' <summary>
'  ''' Full State Snapshot (relevant for serialized transport)
'  ''' </summary>
'  Public Property RawState As Byte()
'    Get

'    End Get
'    Set(value As Byte())

'    End Set
'  End Property

'#End Region

'#Region " Manipulation ('AddClearance') "

'  ''' <summary>
'  ''' BEFORE!!!
'  ''' </summary>
'  Public Event AddingClearance(sender As ClearanceContainer, dimensionName As String, clearanceExpression As String)

'  ''' <summary>
'  ''' AFTER - but only if had not exists before
'  ''' </summary>
'  ''' <param name="sender"></param>
'  ''' <param name="dimensionName"></param>
'  ''' <param name="clearanceExpression"></param>
'  Public Event ClearanceAdded(sender As ClearanceContainer, dimensionName As String, clearanceExpression As String)

'  Public ReadOnly Property Clearances(dimensionName As String) As String()
'    Get

'    End Get
'  End Property

'  Public Function AddClearance(dimensionName As String, clearanceExpression As String) As Boolean

'  End Function

'#End Region

'#Region " Enumeration "

'  Private Iterator Function EnumerateClearences() As IEnumerator(Of Tuple(Of String, String))







'  End Function

'  Public Function GetEnumerator() As IEnumerator(Of Tuple(Of String, String)) Implements IEnumerable(Of Tuple(Of String, String)).GetEnumerator
'    Return Me.EnumerateClearences
'  End Function

'  Private Function GetUntypedEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
'    Return Me.EnumerateClearences
'  End Function

'#End Region

'#Region " 'ToString' & 'TryParse' "

'  Public Shared Function Parse(clearanceSetExpression As String) As ClearanceContainer
'    Dim result As ClearanceContainer = Nothing
'    If (TryParse(clearanceSetExpression, result)) Then
'      Return result
'    End If
'    Throw New FormatException($"Cannot parse the given {NameOf(clearanceSetExpression)}!")
'  End Function

'  Public Shared Function TryParse(clearanceSetExpression As String, ByRef result As ClearanceContainer) As Boolean




'    gruedbydeminsion oder auch nicht




'  End Function

'  Public Overloads Function ToString(groupedByDimension As Boolean) As String





'  End Function

'#End Region

'#Region " System Overrides "

'  Public Overrides Function Equals(obj As Object) As Boolean
'    Return Me.GetHashCode().Equals(obj.GetHashCode())
'  End Function

'  Public Overrides Function GetHashCode() As Integer
'    Return Me.ToString().GetHashCode()
'  End Function

'  Public Overrides Function ToString() As String
'    Return Me.ToString(True)
'  End Function

'#End Region

'End Class

'End Namespace
