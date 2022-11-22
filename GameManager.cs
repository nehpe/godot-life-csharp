using Godot;
using System;
using System.Collections.Generic;

public class GameManager : Node2D
{
	private float zoom = 1.0f;
	private Camera2D cam;
	private const float ZoomStep = 0.1f;
	private Timer timer;

	private const int TILE_W = 32;
	private const int TILE_H = 20;

	private Dictionary<Vector2, Sprite> cells;

	private Sprite cell;

	private Dictionary<Vector2, bool>[] grid;

	private List<Vector2> ToCheck;

	public override void _Ready()
	{
		cam = this.GetNode<Camera2D>("Camera2D");
		timer = this.GetNode<Timer>("Timer");
		cell = this.GetNode<Sprite>("Cell");
		cells = new Dictionary<Vector2, Sprite>();

		grid = new Dictionary<Vector2, bool>[2];
		grid[0] = new Dictionary<Vector2, bool>();
		grid[1] = new Dictionary<Vector2, bool>();

		ToCheck = new List<Vector2>();
		InitialSetUp();
	}

	private void InitialSetUp()
	{
		var rand = new RandomNumberGenerator();
		for (var x = 0; x < TILE_W; x++)
		{
			for (var y = 0; y < TILE_H; y++)
			{
				if (rand.Randf() < 0.5)
				{
					PlaceCell(new Vector2(x * 32, y * 32));
				}
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			var mouseButton = (InputEventMouseButton)@event;
			if (mouseButton.ButtonIndex == (int)ButtonList.Left && mouseButton.IsPressed())
			{
				PlaceCell(mouseButton.Position);
			}
			if (mouseButton.ButtonIndex == (int)ButtonList.Right && mouseButton.IsPressed())
			{
				RemoveCell(mouseButton.Position);
			}
			if (mouseButton.ButtonIndex == (int)ButtonList.WheelDown)
			{
				ChangeZoom(ZoomStep);
			}
			if (mouseButton.ButtonIndex == (int)ButtonList.WheelUp)
			{
				ChangeZoom(-ZoomStep);
			}
		}

		if (@event is InputEventMouseMotion && ((InputEventMouseMotion)@event).ButtonMask == (int)ButtonList.MaskMiddle)
		{
			MoveCamera(((InputEventMouseMotion)@event).Relative);
		}

		if (@event is InputEventKey)
		{
			var keykey = (InputEventKey)@event;
			if (keykey.Scancode == (int)KeyList.Space && keykey.IsPressed())
			{
				GD.Print("Space");
				this.StartStop();
			}
		}
	}

	public void StartStop()
	{
		if (timer.IsStopped() && cells.Count > 0)
		{
			timer.Start();
		}
		else
		{
			timer.Stop();
		}
	}

	public void Reset()
	{
		timer.Stop();
		foreach (Vector2 pos in cells.Keys)
		{
			cells[pos].QueueFree();
		}
		cells.Clear();
	}

	public void ChangeZoom(float dz)
	{
		zoom = Mathf.Clamp((float)(zoom + dz), 0.1f, 8.0f);

		cam.Zoom = new Vector2(zoom, zoom);
	}

	public void MoveCamera(Vector2 dv)
	{
		cam.Offset -= dv;
	}


	private Vector2 GetGridPos(Vector2 pos)
	{
		return pos.Snapped(new Vector2(32, 32)) / 32;
	}

	public void RemoveCell(Vector2 pos)
	{
		var key = this.GetGridPos(pos);

		if (cells.ContainsKey(key))
		{
			cells[key].QueueFree();
			cells.Remove(key);
			grid[1].Remove(key);
		}
	}

	private void _on_Timer_timeout()
	{
		this.invertGrid();
		grid[1].Clear();
		GD.Print("Timer Timeout");
		Regenerate();
		UpdateCells();
		AddNewCells();
		RemoveOld();
	}

	private void RemoveOld()
	{
		foreach (var key in cells.Keys)
		{
			if (!grid[1].ContainsKey(key) || cells[key].Modulate == Colors.Gray)
			{
				cells[key].QueueFree();
			}
		}
	}

	private void AddNewCells()
	{
		foreach (var pos in ToCheck)
		{
			var n = GetNumLiveCells(pos, false);
			AddNewCell(pos);
		}
	}

	public void PlaceCell(Vector2 pos)
	{
		pos = this.GetGridPos(pos);
		if (!cells.ContainsKey(pos))
		{
			GD.Print(pos);
			Sprite localCell = (Sprite)cell.Duplicate();
			localCell.Position = pos * 32;
			AddChild(localCell);
			localCell.Show();
			cells.Add(pos, localCell);
			grid[1].Add(pos, true);
		}
	}

	private void AddNewCell(Vector2 grid_pos)
	{
		if (!cells.ContainsKey(grid_pos))
		{
			var pos = grid_pos * 32;
			GD.Print("new position:" + pos);
			Sprite lcell = (Sprite)cell.Duplicate();
			lcell.Position = pos;
			AddChild(lcell);
			lcell.Show();
			lcell.Visible = true;
			if (!cells.ContainsKey(grid_pos)) cells.Add(grid_pos, lcell);
			if (!grid[1].ContainsKey(grid_pos)) grid[1].Add(grid_pos, true);
		}
	}

	private void Regenerate()
	{
		foreach (var key in cells.Keys)
		{
			var n = GetNumLiveCells(key);
			GD.Print(key + ": " + n);
			if (grid[0][key])
			{
				grid[1][key] = (n == 2 || n == 3);
			}
			else
			{
				grid[1][key] = (n == 3);
			}
		}
	}

	private int GetNumLiveCells(Vector2 key, bool firstPass = true)
	{
		var NumLiveCells = 0;
		foreach (var y in new int[] { -1, 0, 1 })
		{
			foreach (var x in new int[] { -1, 0, 1 })
			{
				if (y != 0 || x != 0)
				{
					var new_pos = key + new Vector2(x, y);
					if (grid[0].ContainsKey(new_pos))
					{
						if (grid[0][new_pos])
						{
							NumLiveCells += 1;
						}
					}
					else
					{
						if (firstPass)
						{
							ToCheck.Add(new_pos);
						}
					}
				}
			}
		}
		return NumLiveCells;
	}

	private void UpdateCells()
	{
		foreach (var key in cells.Keys)
		{
			if (grid[1].ContainsKey(key) && grid[1][key])
			{
				cells[key].Modulate = Colors.AliceBlue;
			}
			else
			{
				cells[key].Modulate = Colors.Gray;
			}
		}

	}

	private void invertGrid()
	{
		var copy = new Dictionary<Vector2, bool>(this.grid[0]);
		this.grid[0] = new Dictionary<Vector2, bool>(grid[1]);
		this.grid[1] = copy;
	}
}
