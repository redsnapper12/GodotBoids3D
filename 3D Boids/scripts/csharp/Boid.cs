using Godot;
using System.Collections.Generic;

public partial class Boid : Node3D
{
	[ExportGroup("Physics")]

	[ExportSubgroup("Forces")]
	[Export] private float _alignmentForce = 12.0f;
	[Export] private float _separationForce = 300.0f;
	[Export] private float _cohesionForce = 0.5f;
	[Export] private float _maxForce = 15.0f;
	[Export] private float _avoidanceThreshold = 10.0f;


	[ExportSubgroup("Physical Properties")]
	[Export] private float _mass = 1.0f;
	[Export] private float _maxSpeed = 25.0f;

	[ExportGroup("Boundary")]
	[Export] private float _boundaryRepulsionForce = 50.0f;
	[Export] private float _edgeBuffer = 15.0f;

	// Spatial Hash Grid
	public SpatialHashGrid3D spatialHashGrid;
	public Vector3I gridIndex = Vector3I.Zero;

	// Boid
	public Vector3 Velocity { get; set; }

	private List<Boid> _neighbors = new();

	private Vector3 _acceleration;
	private Vector3 _steeringForce;


	public override void _Ready()
	{
		Vector3 offset = new(GD.Randf() * 25.0f, GD.Randf() * 25.0f, GD.Randf() * 25.0f);
		GlobalPosition = spatialHashGrid.GetGridCenter() + offset;

		Velocity = new (
			(float)GD.RandRange(-_maxSpeed, _maxSpeed),
			(float)GD.RandRange(-_maxSpeed, _maxSpeed),
			(float)GD.RandRange(-_maxSpeed, _maxSpeed)
		);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Retrieve _neighbors in the grid within a specified range
		_neighbors = spatialHashGrid.GetPrecomputedNeighbors(this);

		// Correct steering if the boid is outside the buffered bounds
		if (!spatialHashGrid.IsPositionInGridBoundsBuffered(Position, _edgeBuffer))
		{
			// Calculate steering back towards the grid center
			Vector3 gridCenter = spatialHashGrid.GetGridCenter();
			Vector3 correctiveSteering = (gridCenter - Position).Normalized() * _boundaryRepulsionForce;

			// Add corrective steering to the boid's velocity
			Velocity += correctiveSteering;
		}

		// Update velocity
		Velocity = GetVelocity();
		_acceleration = Vector3.Zero;

		// Update Position
		Position += Velocity * (float)delta;

		// Look at heading
		if (Position != Vector3.Zero && Velocity != Vector3.Zero) LookAt(Position + Velocity);

		// Clamp inside grid bounds
		Position = Position.Clamp(Vector3.Zero, spatialHashGrid.GetGridBounds());
	}

	private Vector3 GetVelocity()
	{
		// Get vectors for steering rules
		Vector3 separation = GetSeparation() * _separationForce;
		Vector3 alignment = GetAlignment() * _alignmentForce;
		Vector3 cohesion = GetCohesion() * _cohesionForce;

		// DebugDraw3D.DrawArrow(Position, Position + separation, Colors.Red);
		// DebugDraw3D.DrawArrow(Position, Position + alignment, Colors.Blue);
		// DebugDraw3D.DrawArrow(Position, Position + cohesion, Colors.Green);

		// Sum the three rule vectors to get a steering vector
		Vector3 steering = cohesion + separation + alignment;

		// Clamp each axis to not go beyond the max force
		steering = steering.LimitLength(_maxForce);

		//DebugDraw3D.DrawArrow(Position, Position + steering, Colors.Purple);

		// Divide the steering vector by the boid's _mass to get _acceleration
		_acceleration = steering / _mass;

		// Calculate the new velocity by adding our _acceleration to our old velocity, clamp it and return it
		Velocity += _acceleration;

		return Velocity.LimitLength(_maxSpeed);
	}


	private Vector3 GetSeparation()
	{
		Vector3 separation = Vector3.Zero;

		if (_neighbors.Count > 0)
		{
			foreach (Boid boid in _neighbors)
			{
				if (boid.Position.DistanceSquaredTo(Position) < _avoidanceThreshold)
				{
					// Sum the vector pointing away from every neighbor
					separation += Position - boid.Position;
				}
			}

			// Get the vector that points away from the average position of every neighbor
			separation /= _neighbors.Count;
		}

		return separation;
	}

	private Vector3 GetAlignment()
	{
		Vector3 alignment = Vector3.Zero;

		if (_neighbors.Count > 0)
		{
			foreach (Boid boid in _neighbors)
			{
				if (boid.Velocity != Vector3.Zero) // Avoid normalization issues for zero velocity
				{
					alignment += boid.Velocity;
				}

				alignment /= _neighbors.Count;
			}
		}

		return alignment;
	}

	private Vector3 GetCohesion()
	{
		Vector3 cohesion = Vector3.Zero;

		if (_neighbors.Count > 0)
		{
			foreach (Boid boid in _neighbors)
			{
				cohesion += boid.Position;
			}
			cohesion /= _neighbors.Count;
			cohesion -= Position;
		}

		return cohesion;
	}
}
