@echo off
cd /D %~dp0
call ../scripts/set-variables

cd SharpKit.Compiler.Common
call make %1
cd ..

cd SharpKit.Compiler.Java
call make %1
cd ..

cd SharpKit.Compiler.Java.MsBuild
call make %1
cd ..

cd SharpKit.Compiler.JavaScript
call make %1
cd ..

cd SharpKit.Compiler.JavaScript.MsBuild
call make %1
cd ..
