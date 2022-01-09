'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics
Imports System.Text

Namespace ComponentDiscovery

  Public Class Diag

    Private Sub New()
    End Sub

#If DEBUG Then
    Public Shared Property IncludeStacktraces As Boolean = True
#Else
    Public Shared Property IncludeStacktraces As Boolean = False
#End If

    Public Shared Property ErrorNotificationMethod As Action(Of String) = (
    Sub(message As String)
      Trace.TraceError(message)
    End Sub
  )

    Public Shared Property WarningNotificationMethod As Action(Of String) = (
    Sub(message As String)
      Trace.TraceWarning(message)
    End Sub
  )

    Public Shared Property InfoNotificationMethod As Action(Of String) = (
      Sub(message As String)
#If DEBUG Then
        Trace.TraceInformation(message)
#End If
      End Sub
    )

    Public Shared Property VerboseNotificationMethod As Action(Of String) = Nothing

    Friend Shared Sub [Error](ex As Exception)
      If (ErrorNotificationMethod IsNot Nothing) Then
        Dim sb As New StringBuilder()
        DumpException(ex, sb, IncludeStacktraces)
        ErrorNotificationMethod.Invoke(sb.ToString())
      End If
    End Sub

    Friend Shared Sub [Error](message As String)
      If (ErrorNotificationMethod IsNot Nothing) Then
        ErrorNotificationMethod.Invoke(message)
      End If
    End Sub

    Friend Shared Sub Warning(message As String)
      If (WarningNotificationMethod IsNot Nothing) Then
        WarningNotificationMethod.Invoke(message)
      End If
    End Sub

    Friend Shared Sub Info(message As String)
      If (InfoNotificationMethod IsNot Nothing) Then
        InfoNotificationMethod.Invoke(message)
      End If
    End Sub

    Friend Shared Sub Verbose(messageGetter As Func(Of String))
      If (VerboseNotificationMethod IsNot Nothing) Then
        Dim message As String = messageGetter.Invoke()
        VerboseNotificationMethod.Invoke(message)
      End If
    End Sub

    Private Shared Sub DumpException(ex As Exception, target As StringBuilder, includeStacktrace As Boolean)
      If (ex Is Nothing) Then
        Exit Sub
      End If

      'typeinfo and message
      target.AppendLine($"Exception (Type: {ex.GetType().Namespace}.{ex.GetType().Name})")
      target.AppendLine(ex.Message)

      'stacktrace
      If (includeStacktrace) Then
        target.AppendLine("StackTrace:")
        If (ex.StackTrace Is Nothing) Then
          target.AppendLine("[not available]")
        Else
          target.AppendLine(ex.StackTrace)
        End If
      End If

      Try
        'specific details for well known exception types
        Select Case True

          Case TypeOf ex Is ArgumentException
            target.AppendLine($"ParamName: {DirectCast(ex, ArgumentException).ParamName}")

        End Select
      Catch
      End Try

      'inner exceptions
      If (ex.InnerException IsNot Nothing) Then
        target.AppendLine()
        target.AppendLine("################################################################################")
        target.AppendLine()
        target.Append("Inner ")
        DumpException(ex.InnerException, target, includeStacktrace)
      End If

    End Sub

  End Class

End Namespace
