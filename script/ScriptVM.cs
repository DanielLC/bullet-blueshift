using System.Collections;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;
using System.Text.RegularExpressions;

public partial class ScriptVM : RefCounted
{
    private int instructionPointer;
    private Array variables;
    private readonly System.Collections.Generic.Stack<StackElement> stack = new();
    public bool timeToPause;
    public bool nextFrame;  // Only the Player needs this, but it's easier just to add it everywhere.
    public Entity entity;
    private ScriptContext context;
    private Regex missingVariableRegex = new(@"Invalid named index '(\w*)' for base type Object");
    public int InstructionPointer
    {
        get => instructionPointer;
    }
    public ScriptVM(Entity entity, int start = 0, Array vars = null)
    {
        instructionPointer = start;
        variables = vars ?? [];
        this.entity = entity;
        context = new ScriptContext(this);
        entity.scriptContext = context;
    }
    private readonly struct StackElement(Array variables, int jumpTo)
    {
        public readonly Array variables = variables;
        public readonly int returnTo = jumpTo;
    }
    // Runs the script until it reaches a point where it pauses or the script finishes.
    // If it reaches a pause, it returns entity.size before the final point of reference.
    // If it just returned the final event, then you'd have issues where the closer edge of the ship updates too late and seems to suddenly jump.
    // Returns true to continue running later, and false if it's time to die.
    public bool Run()
    {
        if (entity.Path.dead)
        {
            return false;
        }
        timeToPause = false;
        for (int _ = 0; _ < 256; ++_)
        {
            // For consistency's sake, it might be better to automatically end it with a DIE command and not need this.
            if (instructionPointer >= Script.instructions.Count)
            {
                return false;
            }
            if (timeToPause)
            {
                return true;
            }
            var instruction = Script.instructions[instructionPointer];
            // GD.Print("ScriptVM.Run: ", instruction.opCode, ", ", instruction.line);
            // Use break if it's going to the next instruction, or continue if it's jumping.
            switch (instruction.opCode)
            {
                case Script.OpCode.VAR:
                    if (instruction.a == variables.Count)
                        variables.Add(Execute(instruction));
                    else
                        variables[instruction.a] = Execute(instruction);
                    // GD.Print("ScriptVM.Run: ", variables.Count);
                    break;
                case Script.OpCode.SET:
                    variables[instruction.a] = Execute(instruction);
                    // GD.Print("ScriptVM.Run: ", variables[instruction.a]);
                    break;
                case Script.OpCode.RUN:
                    Execute(instruction);
                    break;
                case Script.OpCode.JUMP:
                    instructionPointer = instruction.a;
                    continue;
                case Script.OpCode.JUMP_IF:
                    //GD.Print("ScriptVM.Run JUMP_IF ", instruction.line+1, ": ", string.Join(", ", variables));
                    if ((bool)Execute(instruction))
                        instructionPointer = instruction.a;
                    else
                        instructionPointer = instruction.b;
                    continue;
                case Script.OpCode.GOSUB:
                    {
                        //GD.Print("ScriptVM.Run Before: ", instruction.b, " ", string.Join(", ", variables));
                        Array newVars = (Array)Execute(instruction);
                        stack.Push(new StackElement(variables, instructionPointer + 1));
                        //GD.Print("ScriptVM.Run: pushing variables " + variables + " onto stack");
                        variables = newVars;
                        instructionPointer = instruction.a;
                        //GD.Print("ScriptVM.Run After: ", instruction.b, " ", string.Join(", ", variables));
                        continue;
                    }
                case Script.OpCode.SPAWN:
                    {
                        // TODO: It might be better to loop through this explicitly.
                        Array newVars = variables[0..instruction.b].Duplicate(true);
                        newVars.AddRange((Array)Execute(instruction));
                        var por = entity.GetEndPOR();
                        // If that point of reference doesn't exist, it's presumably an emitter with a dead entity.
                        // But what if it's not? There could be something else that causes this. And with this code, the game won't crash and I'll probably never know.
                        // Called it! Rounding error meant that when trying to run something just barely into the past, it would sometimes be just barely into the future.
                        // I made it only run emitters slightly further into the past, but was that the best way to do it?
                        if (por == null)
                        {
                            // Player.RuntimeError(instructionPointer, "Tried to execute a script where point of reference was null.");
                            GD.Print("ScriptVM.Run: WARNING por = null");
                            return false;
                        }
                        Player.SpawnEntity(0.01f, 0, por, instruction.a, newVars);
                        break;
                    }
                case Script.OpCode.EMITTER:
                    {
                        // TODO: It might be better to loop through this explicitly.
                        Array newVars = variables[0..instruction.b].Duplicate(true);
                        newVars.AddRange((Array)Execute(instruction));
                        //GD.Print("ScriptVM.Run: ", string.Join(", ", newVars));
                        entity.NewEmitter(instruction.a, newVars);
                        break;
                    }
                case Script.OpCode.RETURN:
                    if (stack.Count == 0)
                    {
                        // If there's nothing in the stack, that means it was spawned as a bullet instead of called as a subroutine, so it's time to DIE.
                        return false;
                    }
                    else
                    {
                        var stackElement = stack.Pop();
                        variables = stackElement.variables;
                        /*if (variables == null)
                            GD.Print("ScriptVM.Run: popping empty variables from stack");
                        else
                            GD.Print("ScriptVM.Run: popping variables " + variables + " from stack");*/
                        instructionPointer = stackElement.returnTo;
                        continue;
                    }
                case Script.OpCode.DIE:
                    return false;
            }
            instructionPointer++;
        }
        Player.RuntimeError(instructionPointer, "Infinite loop encountered containing this line.");
        return false;
    }
    public Event RunEntity()
    {
        if (Run())
            return entity.GetEndPOR() * new Event(0, 0, -entity.Size);
        else
            return null;
    }
    private Variant Execute(Script.Instruction instruction)
    {
        // GD.Print("ScriptVM.Execute: line ", instruction.line+1, ", variables ", string.Join(", ", variables));
        Variant result = instruction.expression.Execute(variables, context);
        if (instruction.expression.HasExecuteFailed())
        {
            var errorText = instruction.expression.GetErrorText();
            var match = missingVariableRegex.Match(errorText);
            if (match.Success)
                errorText = $"Variable '{match.Groups[1].Value}' not defined.";
            Player.Error(instruction.line, errorText);
        }
        //GD.Print("ScriptVM.Execute result: ", result);
        return result;
    }
    private void Spawn(Array arguments, int instructionPointer)
    {
        // Setting these to default. I'm probably going to make it so you run a function to change them rather than initially set them to something else.
        float size = 0.1f;
        float rotationSpeed = 0;
        Player.SpawnEntity(size, rotationSpeed, entity.GetEndPOR(), instructionPointer, variables.Duplicate(true));
    }
}
