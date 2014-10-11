@cd /D %~dp0
@call ../scripts/set-variables

IF not "%1" == "release" (

%msbuild% cecil/Mono.Cecil.csproj
%msbuild% NRefactory/NRefactory.sln
%msbuild% corex/corex.sln
%msbuild% AjaxMin/AjaxMinDll/AjaxMinDll.sln
%msbuild% SharpZipLib/src/ICSharpCode.SharpZLib.csproj
%msbuild% octokit.net/Octokit/Octokit-Mono.csproj

) ELSE (

%msbuild% cecil/Mono.Cecil.csproj                         /p:Configuration=net_4_0_Release /p:DebugSymbols=false /p:DebugType=None
%msbuild% NRefactory/NRefactory.sln                       /p:Configuration=net_4_5_Release /p:DebugSymbols=false /p:DebugType=None
%msbuild% corex/corex.sln                                 /p:Configuration=Release         /p:DebugSymbols=false /p:DebugType=None
%msbuild% AjaxMin/AjaxMinDll/AjaxMinDll.sln               /p:Configuration=Release         /p:DebugSymbols=false /p:DebugType=None
%msbuild% SharpZipLib/src/ICSharpCode.SharpZLib.csproj    /p:Configuration=Release         /p:DebugSymbols=false /p:DebugType=None
%msbuild% octokit.net/Octokit/Octokit-Mono.csproj	  /p:Configuration=Release         /p:DebugSymbols=false /p:DebugType=None

)
