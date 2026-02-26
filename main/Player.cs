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
    private Entity entity;    //This was representing the player. The game is not ready for it and it's probably overkill.
    private static Player instance;

    public Player()
    {
        instance = this;
    }

    public override void _Ready()
    {
        try
        {
            //Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            //sprite.ZIndex = 1;
            //sprite.Scale /= 128;

            por = PointOfReference.IDENTITY;

            ScriptedLevel();
            entity = SpawnEntity(PLAYER_SIZE, PLAYER_ROTATION, por, -1);
            entity.NewEmitter(0, []);

        }
        catch (Exception e)
        {
            CallDeferred("CatchError", [e.Message]);
            //CatchError(e);
        }
    }

    private void ScriptedLevel()
    {
        new ScriptCompiler().ParseFile("script.txt");
        //var enemy = SpawnEntity(0.1f, 0f, new Event(0, 0.2f, 0.3f).GetTranslation());
    }

    public override void _Process(double delta)
    {
        try
        {
            //GD.Print($"Player._Process: FPS: {1 / delta}");
            float deltaF = (float)delta;
            //var viewportSize = GetViewportRect().Size;
            //Position = viewportSize / 2;
            //Scale = Mathf.Min(viewportSize.X, viewportSize.Y) * SCALE * Vector2.One;
            var x = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
            var y = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");
            entity.AddAcceleration(Mathf.Sqrt(x * x + y * y) * ACCELERATION, Mathf.Atan2(-x, y), deltaF);
            por = entity.GetEndPOR();
            var dir = new Vector2(x, y).Normalized() * deltaF * ACCELERATION;
            var v = Velocity.FromRapidity(dir.X, dir.Y);
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
            CallDeferred("CatchError", [e.Message]);
        }
    }

    public static Entity SpawnEntity(float size, float rotationSpeed, PointOfReference pointOfReference, int instructionPointer = 0, Godot.Collections.Array variables = null)
    {
        if (instance.GetChildCount() >= SPAWN_CAP)
        {
            // There's too many bullets. Stop spawning them.
            // This should probably be logged somehow? But if there's too many bullets, it's probably going to be a lot too many. Maybe just count the number?
            GD.Print("Player.Spawn: Span cap reached");
            return null;
        }
        Entity entity = (Entity)EntityScene.Instantiate();
        entity.Initialize(instance, size, pointOfReference, rotationSpeed);
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
        if (lineNumber < 0)
            message = $"ERROR: {message}";
        else
            message = $"ERROR on line {lineNumber + 1}: {Script.lines[lineNumber].Trim()}\n{message}";
        throw new Exception(message);
    }

    private void CatchError(string errorText)
    {
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
        text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        text.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var buttons = new HBoxContainer();

        var reload = new Button { Text = "Reload" };
        reload.Pressed += () =>
        {
            // window.QueueFree();
            // GetTree().Paused = false;
            // GetTree().ChangeSceneToFile("res://Player.tscn");
            
            OS.CreateProcess(OS.GetExecutablePath(), new string[] { });
            GetTree().Quit();
        };

        var quit = new Button { Text = "Quit" };
        quit.Pressed += () => GetTree().Quit();

        buttons.AddChild(reload);
        buttons.AddChild(quit);

        vbox.AddChild(text);
        vbox.AddChild(buttons);
        window.AddChild(vbox);

        GetTree().Root.AddChild(window);
        GetTree().Paused = true;
        window.PopupCentered();
    }
}