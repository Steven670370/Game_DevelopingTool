using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

public partial class Protagonist : Node2D
{
	public class Joint
    {
    public Sprite2D Sprite;				// texture
		public Godot.Vector2 FixedPoint;		// position
		public Godot.Vector2[] beFixedPoint; 	// distance and angle about fixed point
		public Godot.Vector2 angle;			// min/max swing angle
		public Joint[] Next;
    }

	class JointPose
	{
    	public Vector2 Move;
    	public float Rotate;
	}

// Called when the node enters the scene tree for the first time.
	Joint joints_group;
	// Parameters describing the body structure in polar coordinates
    // Each Vector2 = (radius, angle)

	Vector2[][] parameters = new Vector2[][]
	{
		new Vector2[]
		{
			// (radius, angle) in polar coordinates
			        	// head
			        	// right arm
			        	// left arm
			         	// right thigh
			         	// left thigh
				  	    // rightForearm
						    // leftForearm
						    // rightShin
					      // leftShin
                // underwear
		},
	};

  // Every time when you want to create an object, just get the node and call it
  	public void InitializeCharacter(int index, Vector2 position)
	{
    	if (joints_group != null || index > parameters.Length)
        	return;

      string basePath = $"res://Textures/Roles/{index}/"; // Just for example
    	joints_group = BuildBody(index, position, parameters[index-1], basePath);
	}
  

	Dictionary<Joint, JointPose> InvertPose(
    Dictionary<Joint, JointPose> src)
{
    var inv = new Dictionary<Joint, JointPose>();

    foreach (var p in src)
    {
        inv[p.Key] = new JointPose
        {
            Move   = -p.Value.Move,
            Rotate = -p.Value.Rotate
        };
    }

    return inv;
}


	// ------------------

	public void Moving_Joint(Joint j, Godot.Vector2 speed) // speed per frame
	{
		if (j == null) return;

		Vector2 changing_rate = new Vector2(speed.X, speed.Y); // let positive y means upward

		j.FixedPoint += changing_rate;
    	j.Sprite.GlobalPosition += changing_rate;

		if (j.Next == null) j.Next = new Joint[0];
    	for (int i = 0; i < j.Next.Length; i++)
    	{
        	Moving_Joint(j.Next[i], speed);
    	}
	}

	public void Rotating_Joint(Joint j, float rotation_rate) // rotating angle per frame
	{
		if (j == null || j.Sprite == null)
        	return;

		Godot.Vector2 dir = j.FixedPoint - j.Sprite.GlobalPosition;
		// 	rotating matrix
    	float cos = Mathf.Cos(rotation_rate);
    	float sin = Mathf.Sin(rotation_rate);
    	Godot.Vector2 rotatedDir = new Godot.Vector2(
        	dir.X * cos - dir.Y * sin,
        	dir.X * sin + dir.Y * cos
    	);

    	// Update position
    	j.Sprite.GlobalPosition = j.FixedPoint - rotatedDir;

    	// Update rotation
    	j.Sprite.Rotation += rotation_rate;

		for (int i = 0; i < j.Next.Length; i++)
    	{
			if (j.beFixedPoint == null || j.beFixedPoint.Length <= i)
        		continue;

        	Joint child = j.Next[i];
			j.beFixedPoint[i].Y += rotation_rate;
        	Godot.Vector2 polar = j.beFixedPoint[i];

			Godot.Vector2 relative = j.FixedPoint + new Vector2(
        		polar.X * Mathf.Cos(polar.Y),
        		polar.X * Mathf.Sin(polar.Y) // since bottom is positive in godot
    		);

			Moving_Joint(child, relative - child.FixedPoint);

			Rotating_Joint(child, rotation_rate);
    	}
	}

	// using generics
	public object ToGodotInput(object input)
	{
		if (Toward)
		{
			if (input is Vector2 v)
    		{
        		return new Vector2(v.X, -v.Y) * CharacterScale;
    		}
    		else if (input is float f)
    		{
        		return -f;
    		}
		}
		else
		{
			if (input is Vector2 v)
    		{
        		return new Vector2(-v.X, -v.Y) * CharacterScale;
    		}
    		else if (input is float f)
    		{
        		return f;
    		}
		}
    	
    	throw new ArgumentException("Unsupported type");
	}

	/// <summary>
	/// Create a Sprite2D from a texture path.
	/// Returns an empty Sprite2D if the texture cannot be loaded.
	/// </summary>
	private Sprite2D CreateSprite(string path)
	{
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(path)
		};
		return sprite;
	}

	/// <summary>
	/// Create a Joint data structure.
	/// A Joint represents a pivot with child joints connected to it.
	/// </summary>
	private Joint CreateJoint(
		Sprite2D sprite,
		Vector2 fixedPoint,
		Vector2[] relativePolarPoints,
		Vector2 angleLimit)
	{
		return new Joint
		{
			Sprite = sprite,
			FixedPoint = fixedPoint,
			beFixedPoint = relativePolarPoints,
			angle = angleLimit
		};
	}

	private Joint BuildBody(int index, Godot.Vector2 Position, Vector2[] parameter, string basePath )
	{
		if(parameter.Length < 10)
		{
			Logger.Error("Lack of parameters. Failed to buid body.");
			return null;
		}

		// --- Create and attach sprites ---
		Sprite2D head        = AddPart(basePath + "Head.png");
		Sprite2D torso       = AddPart(basePath + "Torso.png");

		Sprite2D leftArm     = AddPart(basePath + "Left_Arm.png");
		Sprite2D leftForearm = AddPart(basePath + "Left_Forearm.png");

		Sprite2D rightArm    = AddPart(basePath + "Right_Arm.png");
		Sprite2D rightForearm= AddPart(basePath + "Right_Forearm.png");

		Sprite2D leftThigh   = AddPart(basePath + "Left_Thigh.png");
		Sprite2D leftShin    = AddPart(basePath + "Left_Shin.png");

		Sprite2D rightThigh  = AddPart(basePath + "Right_Thigh.png");
		Sprite2D rightShin   = AddPart(basePath + "Right_Shin.png");

		Sprite2D underwear   = AddPart(basePath + "Underwear.png");

		// --- Torso joint definition ---
		Vector2 torsoSize = torso.Texture.GetSize();

		float Xsize = torsoSize.X / 2;
		float Ysize = torsoSize.Y / 2;

		Joint torsoJoint = CreateJoint(
			torso,
			Position,
			new Vector2[]
			{
				// (radius, angle) in polar coordinates
				new(Ysize*parameter[0].X,  parameter[0].Y * Mathf.Pi),        // head
				new(Ysize*parameter[1].X,  parameter[1].Y * Mathf.Pi),        // right arm
				new(Ysize*parameter[2].X,  parameter[2].Y * Mathf.Pi),        // left arm
				new(Ysize*parameter[3].X,  parameter[3].Y * Mathf.Pi),        // right thigh
				new(Ysize*parameter[4].X,  parameter[4].Y * Mathf.Pi),        // left thigh

				// For underwear
				new(Ysize*parameter[9].X, parameter[9].Y * Mathf.Pi)
			},
			Vector2.Zero
		);
		
		torsoJoint.Sprite.GlobalPosition = torsoJoint.FixedPoint;

		// --- Child joint configuration table ---
		var children = new (Sprite2D sprite, string xCmd, string yCmd)[]
		{
			(head,       "center", "up"),
			(rightArm,   "right",  "bottom"),
			(leftArm,    "left",   "bottom"),
			(rightThigh, "center", "bottom"),
			(leftThigh,  "center", "bottom"),
			(underwear, "center", "bottom")
		};

		// --- Build child joints ---
		var childJoints = new Joint[children.Length];

		for (int i = 0; i < children.Length; i++)
		{
			var (sprite, xCmd, yCmd) = children[i];
			var polar = torsoJoint.beFixedPoint[i];

			Vector2 worldPos = torsoJoint.FixedPoint + new Vector2(
				polar.X * Mathf.Cos(polar.Y),
				polar.X * Mathf.Sin(polar.Y)
			);

			var joint = new Joint
			{
				Sprite = sprite,
				FixedPoint = worldPos
			};

			SetSpritePosition(sprite, polar, torsoJoint.FixedPoint, xCmd, yCmd);

			childJoints[i] = joint;
		}

		torsoJoint.Next = childJoints;


		// for child nodes
		BuildingLimbs(childJoints[1], parameter[5].X, parameter[5].Y, rightForearm);
		BuildingLimbs(childJoints[2], parameter[6].X, parameter[6].Y, leftForearm);
		BuildingLimbs(childJoints[3], parameter[7].X, parameter[7].Y, rightShin);
		BuildingLimbs(childJoints[4], parameter[8].X, parameter[8].Y, leftShin);

		torsoJoint.Next[2].Next[0].Sprite.ZIndex = 30;
		torsoJoint.Next[2].Sprite.ZIndex = 20;

		torsoJoint.Next[4].Next[0].Sprite.ZIndex = 30;
		torsoJoint.Next[4].Sprite.ZIndex = 20;

		torsoJoint.Sprite.ZIndex = 10;

		torsoJoint.Next[3].Next[0].Sprite.ZIndex = 25;
		torsoJoint.Next[3].Sprite.ZIndex = 15;

		torsoJoint.Next[1].Next[0].Sprite.ZIndex = 5;
		torsoJoint.Next[1].Sprite.ZIndex = 0;

		torsoJoint.Next[5].Sprite.ZIndex = 20;

		return torsoJoint;
	}

	private void BuildingLimbs(Joint j, float scaler_1, float scaler_2, Sprite2D s)
	{
		if (j == null || s == null) return;

		float length = j.Sprite.Texture.GetSize().Y * scaler_1;

		Vector2 polar = new Vector2(length, scaler_2 * Mathf.Pi);
		j.beFixedPoint = new Vector2[1];
		j.beFixedPoint[0] = polar;

		// changing polar to global position
		Vector2 worldPos = j.FixedPoint + new Vector2(
			polar.X * Mathf.Cos(polar.Y),
			polar.X * Mathf.Sin(polar.Y)
		);

		j.Next = new Joint[1];
		j.Next[0] = CreateJoint(
			s,
			worldPos,
			null,
			new Vector2(0,0)
		);

		SetSpritePosition(
			j.Next[0].Sprite,
			polar,
			j.FixedPoint,
			"center",
			"bottom"
		);
	}


	/// <summary>
	/// Helper: create sprite and attach it to this node.
	/// Keeps BuildBody readable.
	/// </summary>
	private Sprite2D AddPart(string path)
	{
		var sprite = CreateSprite(path);
		AddChild(sprite);
		return sprite;
	}

	/// <summary>
	/// Convert polar offset into world position and apply visual alignment.
	/// X/Y commands compensate for sprite size so joints connect naturally.
	/// </summary>
	private void SetSpritePosition(
		Sprite2D sprite,
		Vector2 polar,
		Vector2 origin,
		string xAlign,
		string yAlign)
	{
		sprite.GlobalPosition = origin + new Vector2(
			polar.X * Mathf.Cos(polar.Y),
			polar.X * Mathf.Sin(polar.Y)
		);

		Vector2 halfSize = sprite.Texture.GetSize() * 0.5f;

		if (xAlign == "left")  sprite.GlobalPosition -= new Vector2(halfSize.X, 0);
		if (xAlign == "right") sprite.GlobalPosition += new Vector2(halfSize.X, 0);

		if (yAlign == "up")     sprite.GlobalPosition -= new Vector2(0, halfSize.Y);
		if (yAlign == "bottom") sprite.GlobalPosition += new Vector2(0, halfSize.Y);
	}



	int call_times = 0;
	bool isRunning = false;
	bool runStarted = false;
	private Task _currentMotion;
	public async Task Left_is_Pressed(Joint root, bool truth)
	{
		if (_currentMotion != null && !_currentMotion.IsCompleted)
        return;

    	_currentMotion = HandleLeft(root, truth);
    	await _currentMotion;
	}

	private async Task HandleLeft(Joint root, bool truth)
	{
    	if (call_times < 2)
    	{
        	await Walking(root, truth);
        	return;
    	}

    	if (!runStarted)
    	{
        	runStarted = true;
        	isRunning = true;
        	await sRunning(root);
        	return;
    	}

    	isRunning = true;
    	await Running(root, truth);
	}

	public async Task Walking(Joint root, bool truth)
	{
    	var p1 = FirstStepPose(root);
    	var p2 = SecondStepPose(root);
		  var bend = BendPose(root, truth); // real move -> if it moves to the edge of textures of the background
    	var p3 = ThirdStepPose(root);
    	var p4 = FourthStepPose(root);

    	await ApplyPose(p1, 0.2f);
    	await ApplyPose(p2, 0.1f);
		  await ApplyPose(bend, 0.1f);
    	await ApplyPose(p3, 0.1f);
    	await ApplyPose(p4, 0.1f);

		call_times ++;
	}

	bool isApplyingMultiPoses = false;
	public async Task sRunning(Joint root)
	{
		isApplyingMultiPoses = true;

		var p1 = RunningStart(root);

    await ApplyPose(p1, 0.1f);
	}

	public async Task Running(Joint root, bool truth)
	{
		  var p1 = FifthStepPose(root);
    	var p2 = SixthStepPose(root, truth); // real move -> if it moves to the edge of textures of the background
    	var p3 = SeventhStepPose(root);
		  var p4 = EighthStepPose(root);

    	await ApplyPose(p1, 0.05f);
    	await ApplyPose(p2, 0.1f);
    	await ApplyPose(p3, 0.05f);
		  await ApplyPose(p4, 0.1f);
	}

	public async Task eRunning(Joint root)
	{
		  var p1 = RunningEnd(root);

    	await ApplyPose(p1, 0.1f);

		  isApplyingMultiPoses = false;
	}


	public async Task Jumping(Joint root)
	{
    	var p1 = Jumping_Start(root);
		  var p2 = Jumping_Up(root);
		  var p3 = Jumping_Down(root);
		  var p4 = Jumping_End(root);

		  await ApplyPose(p1, 0.1f);
    	await ApplyPose(p2, 0.08f);
    	await ApplyPose(p3, 0.08f);
		  await ApplyPose(p4, 0.1f);
	}

	static bool Toward = true;	// true -> facing right, false -> facing left
	public async Task Turn_Around(Joint root, bool truth)
	{
    	if (root == null) return;

		if (isApplyingMultiPoses || (_currentMotion != null && !_currentMotion.IsCompleted))
        	return;

    	// -----------------------------
    	// Find central X axis of the character
    	// -----------------------------
    	float minX = float.MaxValue;
    	float maxX = float.MinValue;

    	void FindBounds(Joint j)
    	{
        	if (j == null) return;

        	float x = j.Sprite.GlobalPosition.X;
        	minX = Mathf.Min(minX, x);
        	maxX = Mathf.Max(maxX, x);

        	if (j.Next != null)
        	{
            	foreach (var child in j.Next)
                	FindBounds(child);
        	}
    	}

    	FindBounds(root);
    	float centerX = (minX + maxX) / 2f; // central vertical axis

    	// -----------------------------
    	// Mirror all joints around central axis
    	// -----------------------------
   		void MirrorJoint(Joint j, float axisX)
    	{
        	if (j == null) return;

        	// --- Flip FixedPoint horizontally ---
        	float offsetX = j.FixedPoint.X - axisX;
        	j.FixedPoint = new Vector2(axisX - offsetX, j.FixedPoint.Y);

        	// --- Flip relative polar offsets (beFixedPoint) ---
        	if (j.beFixedPoint != null)
        	{
            	for (int i = 0; i < j.beFixedPoint.Length; i++)
            	{
                	Vector2 polar = j.beFixedPoint[i];
                	// horizontal flip: angle -> pi - angle
                	polar.Y = Mathf.Pi - polar.Y;
                	j.beFixedPoint[i] = polar;
            	}
        	}

        	// --- Flip Sprite position horizontally ---
        	offsetX = j.Sprite.GlobalPosition.X - axisX;
        	j.Sprite.GlobalPosition = new Vector2(axisX - offsetX, j.Sprite.GlobalPosition.Y);

        	// --- Flip Sprite image horizontally ---
        	j.Sprite.FlipH = !j.Sprite.FlipH;

       		// --- Recursively flip child joints ---
        	if (j.Next != null)
        	{
            	foreach (var child in j.Next)
                	MirrorJoint(child, axisX);
        	}
    	}

    	MirrorJoint(root, centerX);

   		// Task completed (async required)
    	await Task.CompletedTask;

    	// -----------------------------
    	// Update character direction flag
    	// -----------------------------
    	// true -> facing right, false -> facing left
    	Toward = !Toward;
	}

	// Ensures only one motion is playing at a time
	async Task PlayMotion(Func<Task> motion)
	{
    	if (_currentMotion != null && !_currentMotion.IsCompleted)
        	return;

    	_currentMotion = motion();
    	await _currentMotion;

		_currentMotion = null;
	}

	// -----------------------------------------------
	// Interpolates joints toward a target pose over time
	async Task ApplyPose(
    	Dictionary<Joint, JointPose> target,
    	float duration)
	{
    	if (target == null || target.Count == 0)
        	return;

    	float elapsed = 0f;

   		while (elapsed < duration)
    	{
        	float dt = (float)GetProcessDeltaTime();

        	float remaining = duration - elapsed;
        	float frameDt = Mathf.Min(dt, remaining);

        	elapsed += frameDt;

        	float step = frameDt / duration;

        	foreach (var p in target)
        	{
            	Joint j = p.Key;
            	JointPose pose = p.Value;

            	if (j == null) continue;

            	Vector2 moveStep = pose.Move * step;
            	float rotateStep = pose.Rotate * step;

            	if (moveStep != Vector2.Zero)
                	Moving_Joint(j, moveStep);

            	if (Mathf.Abs(rotateStep) > 0.00001f)
                	Rotating_Joint(j, rotateStep);
        	}

        	await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    	}
	}
	// -----------------------------------------------

	Dictionary<Joint, JointPose> FirstStepPose(Joint root)
	{
    	var pose = new Dictionary<Joint, JointPose>();

    	pose[root.Next[3].Next[0]] = new JointPose
    	{
        	Rotate = (float)ToGodotInput(-0.4f),
        	Move   = (Vector2)ToGodotInput(new Vector2(0, 20))
    	};

    	return pose;
	}

	Dictionary<Joint, JointPose> SecondStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
        	{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(0.2f), Move = (Vector2)ToGodotInput(new Vector2(0, 5))} },
        	{ root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.2f), Move = (Vector2)ToGodotInput(new Vector2(0, 30))} },
        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(-0.1f) } },
        	{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(0.1f) } }
    	};
		}



	Dictionary<Joint, JointPose> BendPose(Joint root, bool truth)
	{
		int moving_x = 0;
		if(truth) moving_x = 40;

		return new Dictionary<Joint, JointPose>
		{
			{root, new JointPose{ Move = (Vector2)ToGodotInput(new Vector2(moving_x, -15))}},
        	{root.Next[3].Next[0], new JointPose{ Rotate = (float)ToGodotInput(0.2f),}},
			{root.Next[4], new JointPose { Rotate = (float)ToGodotInput(-0.2f), Move = (Vector2)ToGodotInput(new Vector2(0, 20))}},
			{root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.2f) }},
			{root.Next[5], new JointPose{ Rotate = (float)ToGodotInput(-0.2f), Move = (Vector2)ToGodotInput(new Vector2(0, 2))}}
		};
	}


	Dictionary<Joint, JointPose> ThirdStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			    {root, new JointPose{ Move = (Vector2)ToGodotInput(new Vector2(0, 15))}},
        	{
            	root.Next[3],
            	new JointPose
            	{
                	Rotate = (float)ToGodotInput(-0.2f),
                	Move   = (Vector2)ToGodotInput(new Vector2(0, -5))
            	}
        	},
        	{ root.Next[4], new JointPose { Rotate = (float)ToGodotInput(0.2f), Move = (Vector2)ToGodotInput(new Vector2(0, -20))}},
        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
			    { root.Next[2], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
			    { root.Next[5], new JointPose{ Rotate = (float)ToGodotInput(0.2f),} }
    	};
	}


	Dictionary<Joint, JointPose> FourthStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
        	{ root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(-0.1f) } },
        	{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(0.1f) } },
			    { root.Next[5], new JointPose{ Move = (Vector2)ToGodotInput(new Vector2(0, -2))} }
    	};
	}

	// -----------------------------------------------

	Dictionary<Joint, JointPose> FifthStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
        	{ root.Next[4], new JointPose {
            	Rotate = (float)ToGodotInput(0.3f),
            	Move   = (Vector2)ToGodotInput(new Vector2(0, 8))
        	}},
        	{ root.Next[4].Next[0], new JointPose {
            	Rotate = (float)ToGodotInput(-0.9f)
        	}},

        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(-0.1f) } },
        	{ root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
        	{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(0.1f) } },
        	{ root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.3f) } },	
    	};
	}

	Dictionary<Joint, JointPose> SixthStepPose(Joint root, bool truth)
	{
    	int moving_x = truth ? 60 : 0;

    	return new Dictionary<Joint, JointPose>
    	{
        	{ root, new JointPose {
            	Move = (Vector2)ToGodotInput(new Vector2(moving_x, -20))
        	}},

        	{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(0.5f) } },
        	{ root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(-Mathf.Pi/2) } },

        	{ root.Next[4], new JointPose {
            	Rotate = (float)ToGodotInput(-0.2f),
            	Move   = (Vector2)ToGodotInput(new Vector2(0, -8))
        	}},
        	{ root.Next[4].Next[0], new JointPose {
            	Rotate = (float)ToGodotInput(0.4f)
        	}},

        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
        	{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
    	};
	}

	Dictionary<Joint, JointPose> SeventhStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
        	{ root, new JointPose {
            	Move = (Vector2)ToGodotInput(new Vector2(0, 10))
        	}},

        	{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(-0.5f) } },
        	{ root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(Mathf.Pi/2) } },

        	{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(-0.1f) } },
        	{ root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
        	{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(0.1f) } },
        	{ root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.3f) } },
    	};
	}

	Dictionary<Joint, JointPose> EighthStepPose(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
        	{ root, new JointPose {
            	Move = (Vector2)ToGodotInput(new Vector2(0, 10))
        	}},

        	{ root.Next[4], new JointPose {
            	Rotate = (float)ToGodotInput(-0.1f)
        	}},
        	{ root.Next[4].Next[0], new JointPose {
            	Rotate = (float)ToGodotInput(0.5f)
        	}},
    	};
	}


	Dictionary<Joint, JointPose> RunningStart(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(-0.15f) } },
      { root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.4f) } },
			{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(-0.15f) } },
      { root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.4f) } },
    	};
	}

	Dictionary<Joint, JointPose> RunningEnd(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root.Next[2], new JointPose { Rotate = (float)ToGodotInput(0.15f) } },
      { root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.4f) } },
			{ root.Next[1], new JointPose { Rotate = (float)ToGodotInput(0.15f) } },
      { root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.4f) } },
    	};
	}
	// -----------------------------------------------

	Dictionary<Joint, JointPose> Jumping_Start(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root, new JointPose { Rotate = (float)ToGodotInput(-0.15f), Move = (Vector2)ToGodotInput(new Vector2(0, -20)) } },
			{ root.Next[0], new JointPose { Move = (Vector2)ToGodotInput(new Vector2(0, -10)) } },
			{ root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.4f) } },
			{ root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.4f) } },
			{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(0.5f) } },
      { root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.35f) } },
			{ root.Next[4], new JointPose { Rotate = (float)ToGodotInput(0.5f) } },
      { root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.75f) } },
    	};
	}

	Dictionary<Joint, JointPose> Jumping_Up(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root, new JointPose { Rotate = (float)ToGodotInput(0.15f), Move = (Vector2)ToGodotInput(new Vector2(0, 150)) } },
			{ root.Next[0], new JointPose { Move = (Vector2)ToGodotInput(new Vector2(0, 10)) } },
			{ root.Next[2].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.4f) } },
			{ root.Next[1].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.4f) } },
			{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(-0.5f) } },
      { root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.35f) } },
			{ root.Next[4], new JointPose { Rotate = (float)ToGodotInput(-0.5f) } },
      { root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.75f) } },
    	};
	}

	Dictionary<Joint, JointPose> Jumping_Down(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root, new JointPose { Move = (Vector2)ToGodotInput(new Vector2(0, -140)) } },
			{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
        	{ root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
			{ root.Next[4], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
        	{ root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(-0.6f) } },
    	};
	}


	Dictionary<Joint, JointPose> Jumping_End(Joint root)
	{
    	return new Dictionary<Joint, JointPose>
    	{
			{ root, new JointPose { Move = (Vector2)ToGodotInput(new Vector2(0, 10)) } },
			{ root.Next[3], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
        	{ root.Next[3].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.2f) } },
			{ root.Next[4], new JointPose { Rotate = (float)ToGodotInput(-0.2f) } },
        	{ root.Next[4].Next[0], new JointPose { Rotate = (float)ToGodotInput(0.6f) } },
    	};
	}

	// -----------------------------------------------

	// State Machine
	// Input handling & motion trigger
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	bool wasPressed = false;
	bool pendingEndRunning = false;
	void HandleRightInput(Joint root, bool truth)
	{
    	bool pressed = Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.Left);

    	if (pressed)
    	{
        	_ = Left_is_Pressed(root, truth);
    	}
    	else if (wasPressed)
    	{
        	if (runStarted || isRunning)
        	{
            	pendingEndRunning = true;
        	}

        	call_times = 0;
        	runStarted = false;
        	isRunning = false;
    	}

    	wasPressed = pressed;

    	if (pendingEndRunning &&
        	(_currentMotion == null || _currentMotion.IsCompleted))
    	{
        	pendingEndRunning = false;
        	_ = PlayMotion(() => eRunning(root));
    	}
	}

	// --------------------
	public float CharacterScale = 1f;
	public void ScaleCharacter(float factor)
	{
    	void ScaleJoint(Joint j)
    	{
       		if (j == null) return;

			CharacterScale = factor;

        	j.FixedPoint *= factor;

        	j.Sprite.GlobalPosition *= factor;

        	if (j.beFixedPoint != null)
        	{
            	for (int k = 0; k < j.beFixedPoint.Length; k++)
            	{
                	Vector2 polar = j.beFixedPoint[k];
                	polar.X *= factor;
                	j.beFixedPoint[k] = polar;
            	}
        	}

        	j.Sprite.Scale *= factor;

        	if (j.Next != null)
        	{
            	foreach (var child in j.Next)
                	ScaleJoint(child);
        	}
    	}

    	ScaleJoint(joints_group);
	}

	// --------------------

	public override void _Process(double delta)
	{
		if(Input.IsKeyPressed(Key.Up))
			_ = PlayMotion(() => Jumping(joints_group));

		if (_currentMotion != null && !_currentMotion.IsCompleted)
        	return;

		bool wantTurn = (Toward && Input.IsKeyPressed(Key.Left)) || (!Toward && Input.IsKeyPressed(Key.Right));

		if (wantTurn)
			_ = PlayMotion(() => Turn_Around(joints_group, true));
		else
    		HandleRightInput(joints_group, true);
	}
}

