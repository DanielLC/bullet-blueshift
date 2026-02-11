using Godot;
using System;
using System.Collections.Generic;

public partial class Script : RefCounted
{
    public enum OpCode : byte
    {      
        VAR,
        SET,
        RUN,
        JUMP,   //Do I need this one? It might be better just to combine it with JUMP_IF.
        JUMP_IF,
        GOSUB,
        SPAWN,
        EMITTER,
        RETURN,
        DIE,
        COUNT
    }
    public class Instruction(Script.OpCode opCode, int line, Expression expression, int a, int b)
    {
        public OpCode opCode = opCode;
        public int line = line;
        public int a = a;
        public int b = b;
        public Expression expression = expression;

    }
    public static List<Instruction> instructions;

    public static void Initialize()
    {
        instructions ??= [];
    }
    public static void AddInstruction(OpCode opCode, int line, Expression expression = null, int a = 0, int b = 0)
    {
        //GD.Print("Script.AddInstruction: ", opCode, ", ", line);
        instructions.Add(new Instruction(opCode, line, expression, a, b));
    }
    public static string[] lines;
}
