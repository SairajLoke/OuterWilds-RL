
dotnet clean
dotnet restore --verbosity detailed
dotnet build -c Release 
dotnet build -c Release --verbosity detailed --no-incremental /warnaserror
# dotnet build -c Release /p:Platform="Any CPU"


./update_mod.ps1