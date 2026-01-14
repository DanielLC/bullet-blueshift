using System.Diagnostics;
using Godot;
using Godot.Collections;

public partial class ScriptVM : RefCounted
{
    private int instructionPointer;
    private Array variables;
    public bool timeToPause;
    public Entity entity;
    private ScriptContext context;
    public ScriptVM(Entity entity, int start = 0)
    {
        instructionPointer = start;
        variables = [];
        this.entity = entity;
        context = new ScriptContext(this);
    }
    // Runs the script until it reaches a point where it pauses or the script finishes.
    // If it reaches a pause, it returns entity.size before the final point of reference.
    // If it just returned the final event, then you'd have issues where the closer edge of the ship updates too late and seems to suddenly jump.
    public Event Run()
    {
        timeToPause = false;
        while (true)
        {
            if (instructionPointer >= Script.instructions.Count)
            {
                return null;
            }
            if (timeToPause)
            {
                return entity.GetEndPOR() * new Event(0, 0, -entity.size);
            }
            var instruction = Script.instructions[instructionPointer];
            Debug.WriteLine("Running command: " + instruction.opCode);
            switch (instruction.opCode)
            {
                case Script.OpCode.VAR:
                    variables.Add(instruction.expression.Execute(variables, context));
                    instructionPointer++;
                    break;
                case Script.OpCode.SET:
                    variables[instruction.a] = instruction.expression.Execute(variables, context);
                    instructionPointer++;
                    break;
                case Script.OpCode.RUN:
                    instruction.expression.Execute(variables, context);
                    instructionPointer++;
                    break;
                case Script.OpCode.JUMP:
                    instructionPointer = instruction.a;
                    break;
                case Script.OpCode.JUMP_IF:
                    if ((bool)instruction.expression.Execute(variables, context))
                        instructionPointer = instruction.a;
                    break;
            }
        }
    }
}
