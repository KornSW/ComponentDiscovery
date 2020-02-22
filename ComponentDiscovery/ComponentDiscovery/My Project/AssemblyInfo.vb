Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

<Assembly: AssemblyTitle("ComponentDiscovery")>
<Assembly: AssemblyDescription("ComponentDiscovery")>
<Assembly: AssemblyCompany("KornSW")>
<Assembly: AssemblyProduct("ComponentDiscovery")>
<Assembly: AssemblyCopyright("KornSW")>
<Assembly: AssemblyTrademark("KornSW")>
<Assembly: ComVisible(False)>
<Assembly: CLSCompliant(True)>
<Assembly: Guid("5027f65d-44ad-44b7-805a-2de61c83390e")>

<Assembly: AssemblyVersion(Major + "." + Minor + "." + Fix + "." + BuildNumber)>
<Assembly: AssemblyInformationalVersion(Major + "." + Minor + "." + Fix + "-" + BuildType)>
'WARNING: DONT SPECIFY: <Assembly: AssemblyFileVersion(...)> 

Public Module SemanticVersion

  'increment this on breaking change:
  Public Const Major = "4"

  'increment this on new feature (w/o breaking change):
  Public Const Minor = "0"

  'increment this on internal fix (w/o breaking change):
  Public Const Fix = "0"

  'AND DONT FORGET TO UPDATE THE VERSION-INFO OF THE *.nuspec FILE!!!
#Region "..."

  'dont touch this, beacuse it will be replaced ONLY by the build process!!!

  Public Const BuildNumber = "*"
  Public Const BuildType = "LOCALBUILD"

#End Region
End Module