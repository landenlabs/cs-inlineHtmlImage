
echo -- Dev common setup  dev-setup.bat 
set dstdir=%bindir%
if not exist "%dstdir%" (
  if exist c:\opt\bin  set dstdir=c:\opt\bin
  if exist d:\opt\bin  set dstdir=d:\opt\bin
)
set bindir=%dstdir%

if not exist "%msbuild%" (
  set msbuild=G:\opt\VisualStudio\18\Community\MSBuild\Current\Bin\MSBuild.exe
)