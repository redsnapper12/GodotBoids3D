using Godot;
using System;
using System.ComponentModel;

[GlobalClass]
public partial class SceneManager : Node3D
{
	[ExportCategory("Spatial Hash Grid")]
	[Export] private int _gridSize = 2;

	[ExportGroup("Distance Querying")]
	[Export] private int _neighborSearchDistance = 2;


	[ExportSubgroup("Debug")]
	[Export] private bool _debug = false;
	[Export] private bool _drawGrid = false;
	[Export] private bool _drawOccupiedCells = false;
	[Export] private bool _drawSearchDistance = false;


	[ExportGroup("Spawning")]
	[Export] private PackedScene _boidScene;
	[Export] private int _maxBoids = 100;


	[ExportCategory("Flock Recognition/Clustering")]
	[Export] private int _flocks = 10;
	[Export] private int _boidSearchDistance = 5;

	private int _boidCount = 0;
	private SpatialHashGrid3D _spatialHashGrid;
	private FlockRecognizer _flockRecognizer;
	
	// DEBUG
	private Vector3 _steps;
	private Vector3I _gridSubdivisions;
	private Vector3 _gridBounds;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		_spatialHashGrid = new SpatialHashGrid3D(_gridSize, _neighborSearchDistance);

		_gridSubdivisions = _spatialHashGrid.GetGridSubdivisions();
		_gridBounds = _spatialHashGrid.GetGridBounds();

		// Get the distance between each layer in all 3 axes.
		_steps = new(_gridBounds.X / _gridSubdivisions.X, _gridBounds.Y / _gridSubdivisions.Y, _gridBounds.Z / _gridSubdivisions.Z);

		// Spawn boids
		for (int n = 0; n < _maxBoids; n++)
		{
			SpawnBoid();
		}

		// Set after spawning boids because the class relies on an accurate boid count
		_flockRecognizer = new FlockRecognizer(_flocks, _boidCount, _spatialHashGrid);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	
		_spatialHashGrid.UpdateGrid();

		// Stop if _debug is false
		if (_debug) 
		{	
			DebugDrawGridOutline();

			// Draw grid
			if(_drawGrid) DebugDrawGrid();

			// Draw occupied grid cells
			if(_drawOccupiedCells) DebugDrawOccupiedCells();

			// Draw each boids search distance
			if(_drawSearchDistance) DebugDrawSearchDistance();
		}	
	}

	private void SpawnBoid()
	{	
		Boid boid = _boidScene.Instantiate<Boid>();
		boid.SetSpatialHashGrid(_spatialHashGrid);

		AddChild(boid);
		_spatialHashGrid.InsertBoid(boid);

		_boidCount++;
	}
	
	private void DebugDrawGrid() {
		DebugDraw3D.ScopedConfig().SetThickness(0.0f);

		// Draw Y grid slices
		for (int y = 0; y <= _gridSubdivisions.Y; y++)
		{
			Transform3D transform = Transform3D.Identity;

			// Position the plane along the Y-axis
			transform.Origin = new Vector3(0, y * _steps.Y, 0);
			transform = transform.Scaled(new(_gridBounds.X, 1, _gridBounds.Z));

			// Draw the grid
			DebugDraw3D.DrawGridXf(transform, new Vector2I(_gridSubdivisions.X, _gridSubdivisions.Y), Colors.Green, false);
		}

		// Draw Z grid slices
		for (int z = 0; z <= _gridSubdivisions.Z; z++)
		{
			Transform3D transform = Transform3D.Identity;

			float z_pos = z * -_steps.Z;
			// Position the plane along the Z-axis
			transform.Origin = new Vector3(0, z_pos + -0.05f, 0);

			// Rotate the plane upwards
			transform = transform.Rotated(Vector3.Right, (float)(1.5 * MathF.PI));

			// Scale the plane to match the bounds
			transform = transform.Scaled(new Vector3(_gridBounds.X, _gridBounds.Y, 1));

			// Draw the grid
			DebugDraw3D.DrawGridXf(transform, new Vector2I(_gridSubdivisions.Y, _gridSubdivisions.Z), Colors.Green, false);
		}
	}

	private void DebugDrawOccupiedCells() 
	{
		// Draw occupied cells
		if (_spatialHashGrid.Boids.Count > 0) 
		{
			// Draw boid cells
			DebugDraw3D.ScopedConfig().SetThickness(0.3f);
			foreach (Boid boid in _spatialHashGrid.Boids)
			{	
				DebugDraw3D.DrawBox(_spatialHashGrid.GetCellPosition(boid.Position), Quaternion.Identity, _spatialHashGrid.CellDimensions, Colors.Red);
			}
		}
	}

	private void DebugDrawSearchDistance()
	{
		// Draw occupied cells
		if (_spatialHashGrid.Boids.Count > 0) 
		{
			// Draw boid cells
			DebugDraw3D.ScopedConfig().SetThickness(0.1f);
			foreach (Boid boid in _spatialHashGrid.Boids)
			{	
				Vector3 cellPosition = _spatialHashGrid.GetCellPosition(boid.Position);
				Vector3 offsetPosition = new(
					cellPosition.X - _spatialHashGrid.CellDimensions.X,
					cellPosition.Y - _spatialHashGrid.CellDimensions.Y,
					cellPosition.Z - _spatialHashGrid.CellDimensions.Z
				);

				DebugDraw3D.DrawBox(
					offsetPosition,
				 	Quaternion.Identity,
					_spatialHashGrid.CellDimensions * (_neighborSearchDistance + 1),
					Colors.Blue
				);
			}
		}
	}

	private void DebugDrawGridOutline()
	{
		DebugDraw3D.ScopedConfig().SetThickness(0.5f);

		DebugDraw3D.DrawBox(
					Vector3.Zero,
				 	Quaternion.Identity,
					_spatialHashGrid.GetGridBounds(),
					Colors.Blue
		);
	}
}
