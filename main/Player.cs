using Godot;
using System;
using System.Diagnostics;

public partial class Player : Node2D
{
    public const float ACCELERATION = 0.5f;
    public const float PLAYER_SIZE = 0.05f;
    public const float PLAYER_ROTATION = 0f;
    public const int SPAWN_CAP = 1000;
    private System.Collections.Generic.List<Tuple<Event, ScriptVM>> events = [];
    //public const float SCALE = 1f;

    public PointOfReference por;
    private static readonly PackedScene EntityScene = (PackedScene)ResourceLoader.Load("res://main/Entity.tscn");
    public Entity playerEntity;
    private ScriptVM playerScript;
    private static Player instance;
    private Exception exception = null;
    private bool hasError = false;
    public static CanvasLayer uiRoot;
    public Explosion explosion;
    private const float EXPLOSION_DURATION = 2f;
    private float explosionTime = float.PositiveInfinity;
	private static readonly PackedScene ExplosionScene = (PackedScene)ResourceLoader.Load("res://main/Explosion.tscn");
    public static bool Paused = false;

	public void Explode()
	{
		explosion = (Explosion)ExplosionScene.Instantiate();
		explosion.Initialize(this, por);
        explosionTime = playerEntity.Path.EndTime;
		//explosion.Initialize(this, size, brightness, Path.GetEndPOR());
		AddChild(explosion);
	}

    public static Player Instance { get { return instance; } }
    public Player()
    {
        instance = this;
    }

    public override void _Ready()
    {
        try
        {
            uiRoot = GetNode<CanvasLayer>("UIRoot");
            RenderingServer.SetDefaultClearColor(new Color(0,0,0));

            por = PointOfReference.IDENTITY;

            ScriptedLevel();
            playerEntity = SpawnEntity(PLAYER_SIZE, PLAYER_ROTATION, por, -1, null, true);
            playerEntity.Name = "Player Entity";
            //playerEntity.NewEmitter(0, []);
            playerScript = new ScriptVM(playerEntity);

        }
        catch (Exception e)
        {
            GD.Print("Player._Ready: Script compiler error");
            exception = e;
            hasError = true;
        }
    }

    private void ScriptedLevel()
    {
        new ScriptCompiler().ParseFile("script.txt");
        //var enemy = SpawnEntity(0.1f, 0f, new Event(0, 0.2f, 0.3f).GetTranslation());
    }

    public override void _Process(double delta)
    {
        if (hasError)
        {
            CatchError(exception);
            exception = null;
            return;
        }
        try
        {
            if(Paused)
                return;
            //GD.Print($"Player._Process: FPS: {1 / delta}");
            float deltaF = (float)delta;
            //var viewportSize = GetViewportRect().Size;
            //Position = viewportSize / 2;
            //Scale = Mathf.Min(viewportSize.X, viewportSize.Y) * SCALE * Vector2.One;
           
            /* var x = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
            var y = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");
            playerEntity.AddAcceleration(Mathf.Sqrt(x * x + y * y) * ACCELERATION, Mathf.Atan2(-x, y), deltaF);
            por = playerEntity.GetEndPOR();
            var dir = new Vector2(x, y).Normalized() * deltaF * ACCELERATION;
            var v = Velocity.FromRapidity(dir.X, dir.Y);
            por.RecalculateInverse();   //I probably don't have to do this every time, but I'll do it for now.*/

            if(playerEntity.Path.EndTime > explosionTime + EXPLOSION_DURATION)
            {
                explosionTime = float.PositiveInfinity;
                explosion.QueueFree();
                explosion = null;
                ScriptContext.DidExplode = true;
                GD.Print("Player._Process: Explosion despawned.");
            }

            for (int _ = 0; !playerScript.nextFrame; ++_)
            {
                if(_ > 256)
                {
                    Error(playerScript.InstructionPointer, "Infinite loop. Did you forget nextFrame()?");
                }
                // TODO: Check if it's false and there's no more code to run.
                playerScript.Run();
            }
            //GD.Print("Player.Run: Loop complete");
            playerScript.nextFrame = false;
            por = playerEntity.GetEndPOR();
            // por isn't being updated? Why?
            // GD.Print("Player.Run: ", playerEntity.GetEnd());
            por.RecalculateInverse();   //I probably don't have to do this every time, but I'll do it for now.

            var curEvent = por.GetEvent();
            for (int i = 0; i < events.Count;)
            {
                if (events[i].Item1 < curEvent)
                {
                    var newEvent = events[i].Item2.RunEntity();
                    if (newEvent == null)
                    {
                        // If it's done, just move the last one here.
                        events[i] = events[^1];
                        events.RemoveAt(events.Count - 1);
                    }
                    else
                    {
                        // Otherwise, use the new position.
                        events[i] = new Tuple<Event, ScriptVM>(newEvent, events[i].Item2);
                    }
                    continue;
                }
                // Only increment i if the event has not updated. Otherwise you'll need to check it again.
                // And you'll need to check if i < events.Count, since that might have changed, which is why I'm continuing the for loop instead of using a while.
                ++i;
            }

            foreach (var child in GetChildren())
            {
                if (child is Entity entity)
                {
                    entity.Update();
                }
            }
        }
        catch (Exception e)
        {
            GD.Print("Player._Process: Script runtime error");
            CatchError(e);
        }
    }

    public static Entity SpawnEntity(float size, float rotationSpeed, PointOfReference pointOfReference, int instructionPointer = 0, Godot.Collections.Array variables = null, bool isPlayer = false)
    {
        if (instance.GetChildCount() >= SPAWN_CAP)
        {
            // There's too many bullets. Stop spawning them.
            // This should probably be logged somehow? But if there's too many bullets, it's probably going to be a lot too many. Maybe just count the number?
            GD.Print("Player.Spawn: Span cap reached");
            return null;
        }
        Entity entity = (Entity)EntityScene.Instantiate();
        entity.Initialize(instance, size, pointOfReference, rotationSpeed, isPlayer);
        instance.AddChild(entity);
        if (instructionPointer > -1)
        {
            ScriptVM script = new(entity, instructionPointer, variables);
            var e = script.RunEntity();
            if (e != null)
                instance.events.Add(new Tuple<Event, ScriptVM>(e, script));
        }
        return entity;
    }

    public static void Error(int lineNumber, string message)
    {
        GD.Print("Player.Error");
        // if (lineNumber < 0)
        //     message = $"ERROR: {message}";
        // else
        //     message = $"ERROR on line {lineNumber + 1}: {Script.lines[lineNumber].Trim()}\n{message}";
        throw new ScriptException(message, lineNumber);
    }
    public static void RuntimeError(int instructionPointer, string message)
    {
        Error(Script.instructions[instructionPointer].line, message);
    }

    private void CatchError(Exception e)
    {
        GD.Print("Player.CatchError");
        string errorText = "";
        if (e is ScriptException scriptException)
        {
            errorText = scriptException.ErrorText;
        }
        else
        {
            errorText = e.ToString();
            errorText = errorText.Replace("\r\n", "\n");
        }
        Debug.WriteLine($"Catching error: {errorText}");
        //var errorText = e.Message;

        var window = new Window();
        window.Title = "Script Error";
        window.Size = new Vector2I(500, 500);
        window.ProcessMode = Node.ProcessModeEnum.Always;

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var text = new TextEdit();
        text.Text = errorText;
        text.Editable = false;
        // This line turns on word wrap instead of having a horizontal scrollbar.
        // I think it's better with the scrollbar, but if you want to change it, just uncomment the line.
        // text.WrapMode = TextEdit.LineWrappingMode.Boundary;
        text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        text.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var buttons = new HBoxContainer();

        var copy = new Button { Text = "Copy to clipboard" };
        copy.Pressed += () => DisplayServer.ClipboardSet(errorText);

        var reload = new Button { Text = "Reload" };
        reload.Pressed += () =>
        {
            // Ideally you'd just reload the scene.

            // window.QueueFree();
            // GetTree().Paused = false;
            // GetTree().ChangeSceneToFile("res://Player.tscn");

            //But I was too lazy to get it to work properly, so here's reloading the entire game.

            OS.CreateProcess(OS.GetExecutablePath(), new string[] { });
            GetTree().Quit();
        };

        var quit = new Button { Text = "Quit" };
        quit.Pressed += () => GetTree().Quit();

        buttons.AddChild(copy);
        buttons.AddChild(reload);
        buttons.AddChild(quit);

        vbox.AddChild(text);
        vbox.AddChild(buttons);
        window.AddChild(vbox);

        GetTree().Root.AddChild(window);
        GetTree().Paused = true;
        window.PopupCentered();
    }

    private class ScriptException(string message, int lineNumber) : Exception(message)
    {
        private readonly int lineNumber = lineNumber;
        public string ErrorText
        {
            get
            {
                if (lineNumber < 0)
                    return $"ERROR: {Message}";
                else
                    return $"ERROR on line {lineNumber + 1}: {Script.lines[lineNumber].Trim()}\n{Message}";
            }
        }
    }
}