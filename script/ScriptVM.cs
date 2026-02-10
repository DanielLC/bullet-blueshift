using System.Collections;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class ScriptVM : RefCounted
{
    private int instructionPointer;
    private Array variables;
    private System.Collections.Generic.Stack<StackElement> stack = new();
    public bool timeToPause;
    public Entity entity;
    private ScriptContext context;
    public ScriptVM(Entity entity, int start = 0, Array vars = null)
    {
        instructionPointer = start;
        variables = vars ?? [];
        this.entity = entity;
        context = new ScriptContext(this);
    }
    private struct StackElement(Array variablesOutOfScope, int variablesInScope, int jumpTo)
    {
        public readonly Array variablesOutOfScope = variablesOutOfScope;
        public readonly int variablesInScope = variablesInScope;
        public readonly int returnTo = jumpTo;
    }
    // Runs the script until it reaches a point where it pauses or the script finishes.
    // If it reaches a pause, it returns entity.size before the final point of reference.
    // If it just returned the final event, then you'd have issues where the closer edge of the ship updates too late and seems to suddenly jump.
    public Event Run()
    {
        timeToPause = false;
        for (int _ = 0; _ < 256; ++_)
        {
            // For consistency's sake, it might be better to automatically end it with a DIE command and not need this.
            if (instructionPointer >= Script.instructions.Count)
            {
                return null;
            }
            if (timeToPause)
            {
                return entity.GetEndPOR() * new Event(0, 0, -entity.size);
            }
            var instruction = Script.instructions[instructionPointer];
            // GD.Print("ScriptVM.Execute:", instruction.line);
            // Debug.WriteLine("ScriptVM.Run(): Running command: " + instruction.opCode);
            // Use break if it's going to the next instruction, or continue if it's jumping.
            switch (instruction.opCode)
            {
                case Script.OpCode.VAR:
                    variables.Add(Execute(instruction.expression));
                    break;
                case Script.OpCode.SET:
                    variables[instruction.a] = Execute(instruction.expression);
                    break;
                case Script.OpCode.RUN:
                    Execute(instruction.expression);
                    break;
                case Script.OpCode.JUMP:
                    instructionPointer = instruction.a;
                    continue;
                case Script.OpCode.JUMP_IF:
                    if ((bool)Execute(instruction.expression))
                        instructionPointer = instruction.a;
                    else
                        instructionPointer = instruction.b;
                    continue;
                case Script.OpCode.SPAWN:
                    {
                        // TODO: It might be better to loop through this explicitly.
                        Array newVars = variables[0..instruction.b].Duplicate(true);
                        newVars.AddRange((Array)Execute(instruction.expression));
                        GD.Print("ScriptVM.Run: ", string.Join(", ", newVars));
                        Player.SpawnEntity(0.01f, 0, entity.GetEndPOR(), instruction.a, newVars);
                        break;
                    }
                case Script.OpCode.GOSUB:
                    {
                        stack.Push(new StackElement(variables[instruction.b..], instruction.b, instructionPointer + 1));
                        variables.Resize(instruction.b);
                        Array newVars = (Array)Execute(instruction.expression);
                        variables.AddRange(newVars);
                        instructionPointer = instruction.a;
                        continue;
                    }
                case Script.OpCode.RETURN:
                    if (stack.Count == 0)
                    {
                        // If there's nothing in the stack, that means it was spawned as a bullet instead of called as a subroutine, so it's time to DIE.
                        return null;
                    }
                    else
                    {
                        var stackElement = stack.Pop();
                        variables = variables[stackElement.variablesInScope..] + stackElement.variablesOutOfScope;
                        instructionPointer = stackElement.returnTo;
                        continue;
                    }
                case Script.OpCode.DIE:
                    return null;
            }
            instructionPointer++;
        }
        throw new System.Exception("Infinite loop");
    }
    private Variant Execute(Expression expression)
    {
        GD.Print("ScriptVM.Execute: ", string.Join(", ", variables));
        Variant result = expression.Execute(variables, context);
        if (expression.HasExecuteFailed())
        {
            throw new System.Exception("Script Execution Failed: " + expression.GetErrorText());
        }
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
