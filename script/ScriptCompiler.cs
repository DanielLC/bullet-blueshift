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
        ELSE,
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

    private bool TryParseAssignment(string line, int lineNumber)
    {
        Match match = assignmentRegex.Match(line);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var expression = new Expression();
            var error = expression.Parse(match.Groups[2].Value, variableNames.ToArray());
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
                    Debug.WriteLine("ScriptCompiler.TryParseAssignment: New variable '" + name + "' at index " + index);
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

    private bool TryParseCommand(string line, int lineNumber)
    {
        Expression expression;
        Match match = commandRegex.Match(line);
        if (match.Success)
        {
            Debug.WriteLine("ScriptCompiler.TryParseCommand: " + match.Groups[1].Value.ToLower());
            switch (match.Groups[1].Value.ToLower())
            {
                case "if":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value, variableNames.ToArray());
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP_IF, expression, Script.instructions.Count + 1));
                    stack.Push(new StackMemory(Jump.IF, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "while":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value, variableNames.ToArray());
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP_IF, expression, Script.instructions.Count + 1));
                    stack.Push(new StackMemory(Jump.WHILE, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "spawn":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value, variableNames.ToArray());
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.SPAWN, null, 0, 1));
                    stack.Push(new StackMemory(Jump.WHILE, Script.instructions.Count - 1, variableNames.Count));
                    break;
                case "else":
                    var memory = stack.Pop();
                    variableNames.RemoveRange(memory.variableCount, variableNames.Count - memory.variableCount);
                    if(memory.command != Jump.IF)
                        throw new Exception("Error on line " + lineNumber + ": else command outside of if statement.");
                    // Add a JUMP, which will be redirected to the next END.
                    Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP, null));
                    // Redirect the IF to point where you are now.
                    Script.instructions[memory.position].b = Script.instructions.Count;
                    // Make the next END redirect this JUMP to point there.
                    stack.Push(new StackMemory(Jump.ELSE, Script.instructions.Count - 1, variableNames.Count));
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
        Debug.WriteLine("ScriptCompiler.ParseEnd: " + memory.command);
        switch (memory.command)
        {
            case Jump.IF:
                Script.instructions[memory.position].b = Script.instructions.Count;
                Debug.WriteLine("ScriptCompiler.ParseEnd: " + Script.instructions[memory.position].a + " " + Script.instructions[memory.position].b);
                break;
            case Jump.ELSE:
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
            case Jump.WHILE:
                Script.instructions.Add(new Script.Instruction(Script.OpCode.JUMP, null, memory.position));
                Script.instructions[memory.position].b = Script.instructions.Count;
                break;
        }
        variableNames.RemoveRange(memory.variableCount, variableNames.Count - memory.variableCount);
    }

    public void ParseFile(string filename)
    {
        Script.Initialize();
        using var reader = new StreamReader(filename);
        string line;
        int lineNumber = 0;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            //Empty line
            if (line.Length == 0) continue;
            //Comment
            else if (line.StartsWith('#')) continue;
            //Command (<Command> <Expression>, <Expression>, ...)
            else if (TryParseCommand(line, lineNumber)) continue;
            //Assignment (variableName = <Expression>)
            else if (TryParseAssignment(line, lineNumber)) continue;
            lineNumber++;
        }
        if(stack.Count > 0)
        {
            throw new Exception("Script is missing END command");
        }
    }
}
