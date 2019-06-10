chdir /D src\app

.paket\paket.exe install --redirects --clean-redirects
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe install --redirects --clean-redirects

chdir /D MasterDbLib.Function

dotnet restore
dotnet build