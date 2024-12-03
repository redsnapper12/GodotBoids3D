using Godot;
using System;

public partial class FlockRecognizer : Node
{
    private SpatialHashGrid3D _spatialHashGrid = null;

    // n - observations
    private int _boidCount = 0;

    // k - clusters
    private int _flocks = 0;

    public FlockRecognizer(int flocks, int boidCount, SpatialHashGrid3D grid) 
    {
        _flocks = flocks;
        _boidCount = boidCount;
        _spatialHashGrid = grid;
    }

    public void UpdateFlockRecognizer() 
    {

    }

    private void InitializeFlocks() 
    {
        for(int k = 0; k < _flocks; k++) 
        {
            
        }
    }
}
