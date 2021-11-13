'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/ComponentDiscovery                           ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Namespace Composition.InstanceDiscovery

  Friend Enum InjectionDemand As Integer

    ''' <summary>
    ''' If the instance discovery fails, then the framework will throw an Exception!
    ''' </summary>
    SuccessOrThrow = 0

    ''' <summary>
    ''' If the instance discovery fails, then a null-refernce will be passed!
    ''' </summary>
    SuccessOrNull = 1

    ''' <summary>
    ''' If the instance discovery fails, then the framework shlud not provide anything!
    ''' (only possible when the injection-target is a MEMBER or a OPTIONAL-PARAMETER)
    ''' </summary>
    SuccessOrSkip = 2

    ''' <summary>
    ''' used as default for OPTIONAL-PARAMETERs which dont have an injection attribute
    ''' </summary>
    SkipAlways = 3

  End Enum

End Namespace
