handy cmds ---------------------------


dotnet list ShipLogger.csproj package
dir bin\Release\net48\ShipLogger.dll
dotnet clean
dotnet restore --verbosity detailed
dotnet build -c Release 
dotnet build -c Release --verbosity detailed --no-incremental /warnaserror


load mod load, start game ( it stops due to the account stuff)
run the patched outerwilds.exe file

---------------------------------------



#Todo
### Game Interaction
1. access ship coords - done 
2. access planet coords 
3. speedup
4. figure out reset episode function for RL env 

### RL 
5. figure out {state + obs + reward + actions} 
    - what input to give ( current plan : ship pose(loc+orient) +  loc of planets + masses of planet + acce? )