using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public partial class ScriptCompiler : Node
{
    readonly Regex assignmentRegex = new Regex(@"^\s*(?:(\w+)\s*=\s*)?(.+)$");  // [variable =]? expression
    readonly Regex commandRegex = new Regex(@"^\s*(\w+)\s*(.*)$");       // command [expression]
    public List<string> variableNames = [];
    private enum Jump : byte
    {
        IF,
        // For makes things a lot more complicated. Maybe I'll come back to it later. It will likely involve restructuring.
        WHILE,
        SUBROUTINE,
        COUNT
    }
    private class StackMemory
    {
        public int position;
        public int variableCount;
        public Jump command;
        public StackMemory(Jump command, int position, int variableCount)
        {
            this.position = position;
            this.variableCount = variableCount;
            this.command = command;
        }
        public List<int> breaks;
    }
    private readonly Stack<StackMemory> stack = new();

    private bool TryParseAssignment(string line)
    {
        Match match = assignmentRegex.Match(line);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var expression = new Expression();
            var error = expression.Parse(match.Groups[2].Value, variableNames.ToArray());
            Debug.WriteLine(error);
            if (error != Error.Ok)
            {
                throw new Exception("Parsing error " + error + " on line: " + line);
            }
            if (varName == "")
            {
                Script.instructions.Add(new Script.Instruction(Script.OpCode.RUN, expression));
            }
            else
            {
                var name = match.Groups[1].Value;
                var index = variableNames.IndexOf(name);
                if (index == -1)
                {
                    index = variableNames.Count;
                    variableNames.Add(name);
                    Debug.WriteLine("New variable '" + name + "' at index " + index);
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.VAR, expression, index));
                }
                else
                {
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.SET, expression, index));
                }
            }
        }
        return match.Success;
    }

    private void ParseBasicInstruction(Match match, Script.OpCode opCode)
    {
        Expression expression = new Expression();
        expression.Parse("[" + match.Groups[2].Value + "]");
        Script.instructions.Add(new Script.Instruction(opCode, expression));
    }

    private bool TryParseCommand(string line)
    {
        Expression expression;
        Match match = assignmentRegex.Match(line);
        if (match.Success)
        {
            switch (match.Groups[1].Value.ToLower())
            {
                case "if":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value);
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP_IF, expression));
                    stack.Push(new StackMemory(Jump.IF, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "while":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value);
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP_IF, expression));
                    stack.Push(new StackMemory(Jump.WHILE, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "spawn":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value);
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.SPAWN, null, 0, 1));
                    stack.Push(new StackMemory(Jump.WHILE, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "end":
                    ParseEnd();
                    break;
                default:
                    return false;
            }
        }
        return match.Success;
    }

    private void ParseEnd()
    {
        var memory = stack.Pop();
        switch (memory.command)
        {
            case Jump.IF:
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
            case Jump.WHILE:
                Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP, null, memory.position));
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
        }
        variableNames.RemoveRange(memory.variableCount, stack.Count - memory.variableCount);
    }

    public void ParseFile(string filename)
    {
        Script.Initialize();
        using var reader = new StreamReader(filename);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            //Empty line
            if (line.Length == 0) continue;
            //Comment
            else if (line.StartsWith('#')) continue;
            //Command (<Command> <Expression>, <Expression>, ...)
            else if (TryParseCommand(line)) continue;
            //Assignment (variableName = <Expression>)
            else if (TryParseAssignment(line)) continue;
        }
    }
}
