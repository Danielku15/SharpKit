@cd /D %~dp0
@call ../../scripts/set-variables

IF not "%1" == "release" (

%msbuild% SharpKit.Compiler.Java.MsBuild.csproj

) ELSE (

%msbuild% SharpKit.Compiler.Java.MsBuild.csproj /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=None

)