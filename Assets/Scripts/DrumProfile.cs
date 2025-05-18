using System;
using System.Collections.Generic;

[Serializable]
public class DrumProfile
{
    public string className;
    public List<float[]> Snapshots = new();
}