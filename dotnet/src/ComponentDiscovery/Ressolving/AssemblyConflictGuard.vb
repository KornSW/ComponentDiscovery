Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection

Namespace ComponentDiscovery

  Public Class AssemblyConflictGuard

    Public Sub New()
    End Sub

    Private Shared _Enabled As Boolean = False

    Private Shared _Cache As New Dictionary(Of String, String)

    Private Shared _Initializing As Boolean = False

    Public Shared Property Enabled As Boolean
      Get
        Return _Enabled
      End Get
      Set(value As Boolean)
        If (Not _Enabled = value) Then
          _Enabled = value
          If (_Enabled) Then
            AddHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf AppDomain_AssemblyLoad
            _Initializing = True
            Try
              For Each a In AppDomain.CurrentDomain.GetAssemblies().ToArray()
                HandleNewAssembly(a)
              Next
            Finally
              _Initializing = False
            End Try
          Else
            RemoveHandler AppDomain.CurrentDomain.AssemblyLoad, AddressOf AppDomain_AssemblyLoad
            _Cache.Clear()
          End If
        End If
      End Set
    End Property

    Private Shared Sub AppDomain_AssemblyLoad(sender As Object, args As AssemblyLoadEventArgs)
      HandleNewAssembly(args.LoadedAssembly)
    End Sub

    Private Shared Sub HandleNewAssembly(ass As Assembly)
      Dim name As String = ass.GetName().Name.ToLower

      'notwendig, da beim hochfahren der appdomain bereits die .config datei gelesen wird
      'und wir diesem prozess nicht in die quere kommen dürfen...
      If (name = "mscorlib" OrElse name.StartsWith("system") OrElse name.EndsWith(".resources")) Then
        Exit Sub
      End If

      'If (Global.My.Settings.AssemblyConflictProtectionMode = 0) Then
      '  Exit Sub
      'End If

      If ass.IsDynamic Then
        'Dynamische Assemblys werden ggf. mehrfach generiert.
        Exit Sub
      End If

      Dim stString As String

      If (ass.IsDynamic) Then
        stString = "located only in Memory (dynamic assembly)"
      Else
        stString = $"located at '{ass.Location}'"
      End If

      If (_Initializing) Then
        stString = stString + " was loaded during initialization"
        'ElseIf (Global.My.Settings.AssemblyConflictProtectionMode = 2) Then
        '  Try
        '    Dim st As New StackTrace(True)
        '    Dim sr As New StringReader(st.ToString())

        '    'fetch 3 lines form the stacktrace (which are inside of our class)
        '    sr.ReadLine()
        '    sr.ReadLine()
        '    sr.ReadLine()

        '    stString = stString + " was loaded on demand" + sr.ReadToEnd()
        '  Catch
        '  End Try
      Else
        stString = stString + " was loaded on demand"
      End If

      SyncLock _Cache

        If (_Cache.ContainsKey(name)) Then

          'For Each whitelistEntry In Global.My.Settings.AssemblyConflictWhitelist.Split(";"c)
          '  If (Not String.IsNullOrWhiteSpace(whitelistEntry) AndAlso whitelistEntry.Trim().ToLower() = name.ToLower()) Then
          '    Exit Sub
          '  End If
          'Next

          Dim errorMessage As String = (
            $"The AssemblyConflictGuard detected a duplicate assembly named '{name}' " +
            $"which was loaded twice into the current AppDomain! The first one {_Cache(name)} and " +
            $"the second one {stString}."
          )

          Diag.Error(errorMessage)
          Try
            'DevLogger.LogError(2086567509961995472L, 0, errorMessage)
          Catch
          End Try

          Throw New Exception(errorMessage)
        Else
          _Cache.Add(name, stString)
        End If

      End SyncLock

    End Sub

  End Class

End Namespace
