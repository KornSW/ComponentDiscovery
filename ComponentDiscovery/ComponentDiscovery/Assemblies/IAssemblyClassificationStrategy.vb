
Public Interface IAssemblyClassificationStrategy

    ''' <summary>
    '''   Occurs when clearance expressions have actually been added to the ClearanceLabels collection.
    ''' </summary>
    ''' <param name="addedExpressions"> An array of the effectively added labels. </param>
    Event ClearancesAdded(addedExpressions As String())

    Function GetClassifications(assemblyFullFilename As String) As String()

    ''' <summary>
    '''   Checks if all(!) classification expressions of an assembly are currently present in the ClearanceLabels collection.
    ''' </summary>
    ''' <param name="assemblyFullFilename"> The assembly to verify. </param>
    ''' <returns> True, if the assembly matches all clearances. </returns>
    Function VerifyAssembly(assemblyFullFilename As String) As Boolean

    ''' <summary>
    '''   Adds the classification expressions of an assembly to the ClearanceLabels collection.
    '''   This will instantly make the assembly approvable.
    ''' </summary>
    ''' <returns>
    '''   True, if at least one new expression has actually been added to the ClearanceLabels collection.
    ''' </returns>
    Function AddClearancesFromAssembly(assemblyFullFilename As String) As Boolean

    ''' <summary>
    '''   Adds further labels to the ClearanceLabels collection. Duplicates will be ignored.
    '''   Expanding the clearance labels collection will broaden the set of approvable assemblies.
    ''' </summary>
    ''' <returns>
    '''   True, if at least one new expression has actually been added to the ClearanceLabels collection.
    ''' </returns>
    Function AddClearances(ParamArray clearanceExpressions() As String) As Boolean

    ReadOnly Property Clearances As String()

  End Interface
