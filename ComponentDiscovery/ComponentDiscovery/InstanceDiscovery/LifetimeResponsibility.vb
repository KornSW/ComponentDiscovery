
''' <summary>
''' Defines which party (the provider or the consumer) has the responsibility to manage the lifecycle of an instance.
''' </summary>
Public Enum LifetimeResponsibility As Integer

  ''' <summary>
  ''' Defines, that the lifetime of an instance is managed internally by the provider.
  ''' This means, that the consumer should treat the exposed instance like a SINGLETON
  ''' and MUST NOT DISPOSE it!
  ''' </summary>
  Managed = 0

  ''' <summary>
  ''' Defines, that any lifetime handling of a exposed instance is delegated to the consumer.
  ''' This means, that the consumer should treat the exposed instance like it CAME FROM A FACTORY
  ''' which includes a consumer-side responsibility to TAKE CARE OF CORRECT DISPOSAL.
  ''' </summary>
  Delegated = 1

End Enum
