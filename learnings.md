### Outer wilds game specific

Mod Helper: utils, https://owml.outerwildsmods.com/mod_helper/manifest.html

What is the coordinate system used? player centric???
  
Decompiled Assembly-CSharp.dll:
1. use Locator to get infor on game objects
2. ShipBody has vel/pos/orient set methods

public Vector3 GetRelativeVelocity(OWRigidbody relativeBody)
	{
		return relativeBody.GetVelocity() - this.GetVelocity();
	}
pssibly useful ...,get normal and tangetial components of rel vel...i need to set vNor to 0, and tangential to vTau setpoint


impt : _rigidbody.velocity is in unity coords, for vel wrt unive they have given GetVelocity method 
### Unity /.NET / specific 


nudget: ZIP file with compiled C# DLLs + metadata
https://learn.microsoft.com/en-us/nuget/what-is-nuget

dotnet: 

