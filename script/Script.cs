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
        SPAWN,
        COUNT
    }
    public class Instruction
    {
        public OpCode opCode;
        public int a;
        public int b;
        public Expression expression;

        public Instruction(OpCode opCode, Expression expression, int a = 0, int b = 0)
        {
            this.opCode = opCode;
            this.a = a;
            this.b = b;
            this.expression = expression;
        }

    }
    public static List<Instruction> instructions;

    public static void Initialize()
    {
        instructions ??= [];
    }
}
