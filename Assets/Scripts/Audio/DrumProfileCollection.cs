using System;
using System.Collections.Generic;

[Serializable]
public class DrumProfileCollection
{
    public List<DrumProfile> profiles = new();

    public DrumProfile GetProfile(string className)
    {
        return profiles.Find(p => p.className == className);
    }         
}