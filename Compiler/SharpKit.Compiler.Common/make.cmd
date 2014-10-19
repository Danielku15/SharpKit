@cd /D %~dp0
@call ../../scripts/set-variables

IF not "%1" == "release" (

%msbuild% SharpKit.Compiler.Common.csproj

) ELSE (

%msbuild% SharpKit.Compiler.Common.csproj /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=None

)