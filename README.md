# OW-RL
RL agent to control OW ship

#### To build mod and place in owml dir run
.\build.ps1


---------------------------------------

# Todo

### Game Interaction
1. access ship coords - done 
2. access planet coords 
3. speedup
4. figure out reset episode function for RL env 

### RL 
5. figure out {state + obs + reward + actions} 
    - what input to give ( current plan : ship pose(loc+orient) +  loc of planets + masses of planet + acce? )


### others
1. make , publish mod to data repo
2. make vid demo
### References
https://owml.outerwildsmods.com/guides/patching.html : on classes used 
https://owml.outerwildsmods.com/guides/patching.html#getting-the-object-youre-patching

https://github.com/ow-mods/outer-wilds-unity-wiki/wiki/Other-%E2%80%90-Global-Messenger-Reference : possibly useful to det events and design rewards/penalties

https://github.com/ow-mods/ow-mod-template

https://owml.outerwildsmods.com/guides/mod_settings.html#getting-values-in-c : mod settings


Useful refs till now
https://owml.outerwildsmods.com/guides/
https://github.com/ow-mods/outer-wilds-unity-wiki/


### handy cmds ---------------------------
dotnet list ShipLogger.csproj package
dir bin\Release\net48\ShipLogger.dll
dotnet clean
dotnet restore --verbosity detailed
dotnet build -c Release 
dotnet build -c Release --verbosity detailed --no-incremental /warnaserror


load mod load, start game ( it stops due to the account stuff)
run the patched outerwilds.exe file
---------------------------------------

