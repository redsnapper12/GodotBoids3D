using Godot;
using System;
using System.Collections.Generic;

public partial class SpatialHashGrid3D
{	
	public List<Boid> Boids { get; set; }
	public Dictionary<Boid, List<Boid>> preComputedNeighbors = new();
	public Vector3 CellDimensions { get; set; }


	// The amount of cells in the X, Y, and Z axes
	private Vector3I _gridSubdivisions = new(20, 20, 20);

	// The physical coordinate bounds of the grid
	private Vector3 _gridBounds = new(100, 100, 100);

	private readonly int _neighborSearchDistance;

	private readonly Dictionary<Vector3I, List<Boid>> gridData = new();

	/*
		Public Functions
	*/

	public SpatialHashGrid3D(int gridSize, int neighborSearchDistance) 
	{
		_gridSubdivisions *= gridSize;
		_gridBounds *= gridSize;
		_neighborSearchDistance = neighborSearchDistance;

		Boids = new();

		// Calculate cell subdivisions
		CellDimensions = new Vector3(_gridBounds.X / _gridSubdivisions.X,
		 _gridBounds.Y / _gridSubdivisions.Y,
		 _gridBounds.Z / _gridSubdivisions.Z
		 );

		InitializeGridCells();
	}

	public void UpdateGrid() 
	{
		// Clear all cells before we update to ensure spatial data parity
		ResetCells();

		// Determine each boids grid position
		foreach (Boid boid in Boids) {
			boid.gridIndex = GetCellIndex(boid.Position);
			List<Boid> cell = gridData[boid.gridIndex];
			cell.Add(boid);
		}

		PreComputeNeighbors();
	}

	public void InsertBoid(Boid boid)
	{
		Boids.Add(boid);
		GD.Print(Boids.Count);
	}
	
	public List<Boid> GetPrecomputedNeighbors(Boid boid) 
	{
		// Return neighbors if boid exists in dictionary, else return empty list
		return preComputedNeighbors.ContainsKey(boid) ? preComputedNeighbors[boid] : new List<Boid>();
	}

	public List<Boid> GetCell(Boid exclusion, Vector3I gridIndex)
	{
		List<Boid> cell = gridData[gridIndex];
		cell.Remove(exclusion);

		return cell;
	}

	public List<Boid> GetNearby(Vector3I gridIndex, int range, bool excludeGridIndex) 
	{
		List<Boid> neighbors = new();

		for (int x = gridIndex.X - range; x < gridIndex.X + range - 1.0; x++)
		{
			for (int y = gridIndex.Y - range; y < gridIndex.Y + range - 1.0; y++)
			{
				for (int z = gridIndex.Z - range; z < gridIndex.Z + range - 1.0; z++)
				{
					Vector3I index = new(x, y, z);

					if (!IsIndexInBounds(index)) 
					{
						continue;
					}

					// Check if index is the cell we are searching from,
					// then check if we have chosen to exclude said cell
					if(index == gridIndex && excludeGridIndex)
					{
						continue;
					}

					// If cell is not empty, append neighbors
					if (gridData[index].Count > 0) 
					{
						neighbors.AddRange(gridData[index]);
					}
				}
			}
		}

		return neighbors;
	}

	public Vector3 GetGridCenter() 
	{
		return _gridBounds / 2.0f;
	}

	public Vector3 GetGridBounds() 
	{
		return _gridBounds;
	}

	public Vector3I GetGridSubdivisions() 
	{
		return _gridSubdivisions;
	}

	public Vector3 GetCellPosition(Vector3 position) 
	{	
		Vector3I idx = GetCellIndex(position);
		return new(idx.X * CellDimensions.X, idx.Y * CellDimensions.Y, idx.Z * CellDimensions.Z);
	}

	public bool IsPositionInGridBounds(Vector3 position) 
	{
		return position.X >= 0.0f && position.X <= _gridBounds.X &&
           position.Y >= 0.0f && position.Y <= _gridBounds.Y &&
           position.Z >= 0.0f && position.Z <= _gridBounds.Z;
	}

	public bool IsPositionInGridBoundsBuffered(Vector3 position, float buffer) 
	{
		return position.X >= 0.0f + buffer && position.X <= _gridBounds.X - buffer &&
           position.Y >= 0.0f + buffer && position.Y <= _gridBounds.Y - buffer &&
           position.Z >= 0.0f + buffer && position.Z <= _gridBounds.Z - buffer;
	}


	/*
		Private Functions
	*/

	private void PreComputeNeighbors() 
	{
		preComputedNeighbors.Clear();

		foreach (Boid boid in Boids)
		{
			List<Boid> neighbors = GetNearby(boid.gridIndex, _neighborSearchDistance, false);
			neighbors.Remove(boid);

			preComputedNeighbors[boid] = neighbors;
		}
	}

	private bool IsIndexInBounds(Vector3I index) 
	{
		for (int dimension = 0; dimension < 3; dimension++)
		{
			if(!(index[dimension] > 0 && index[dimension] < _gridSubdivisions[dimension]))
			{
				return false;
			}
		}

		return true;
	}


	private Vector3I GetCellIndex(Vector3 position) 
	{	
		Vector3I idx = Vector3I.Zero;

		for (int dim = 0; dim < 3; dim++) 
			idx[dim] = (int)Math.Clamp(Math.Floor(position[dim] / CellDimensions[dim]), 0.0, _gridSubdivisions[dim] - 1.0);
		
		return idx;
	}

	private void ResetCells() 
	{
		foreach (KeyValuePair<Vector3I, List<Boid>> entry in gridData)
		{
			List<Boid> cell = entry.Value;
			cell.Clear();
		}
	}

	private void InitializeGridCells()
	{
		// Traverse the grid and create a new array for each cell
		for (int x = 0; x < _gridSubdivisions.X; x++) 
		{
			for (int y = 0; y < _gridSubdivisions.Y; y++)
			{
				for (int z = 0; z < _gridSubdivisions.Z; z++)
				{
					gridData.Add(new Vector3I(x, y, z), new List<Boid>());
				}
			}
		}
	}
}
