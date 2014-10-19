@cd /D %~dp0
@call ../../scripts/set-variables

IF not "%1" == "release" (

%msbuild% SharpKit.Compiler.JavaScript.csproj

) ELSE (

%msbuild% SharpKit.Compiler.JavaScript.csproj /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=None

)