
''' <summary>
''' selects the required behavior, when trying to provide instances for requrested types
''' </summary>
Public Enum InjectionDemand As Integer

  ''' <summary>
  ''' This will instruct the framework to discover and inject an instance into each
  ''' non-optional constructor-/method-argument and/or property with an 'ProvidesDiscoverableInstanceAttribute'.
  ''' If any instance cant be discovered, then the framework will throw an Exception!
  ''' </summary>
  Required = 1

  ''' <summary>
  ''' This will instruct the framework to discover and inject an instance into each
  ''' non-optional constructor-/method-argument and/or property with an 'ProvidesDiscoverableInstanceAttribute'.
  ''' If any instance cant be discovered, then the framework will pass null for these parameters / will skip setting the property!
  ''' </summary>
  IfAvailable = 2

  Disabled = 0

End Enum
