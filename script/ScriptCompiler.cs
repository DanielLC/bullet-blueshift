using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class ScriptCompiler : Node
{
    readonly Regex assignmentRegex = new Regex(@"^\s*(?:(\w+)\s*=\s*)?(.+)$");  // [variable =]? expression
    readonly Regex commandRegex = new Regex(@"^\s*(\w+)\s*(.*)$");              // command [expression]
    readonly Regex subroutineRegex = new Regex(@"^\s*SUBROUTINE\s+(\w+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);   // name [parameter,]* [parameter]
    private List<string> variableNames = [];
    // I need a list of subroutine names so that it can detect when you're writing to a subroutine. Then I need to store a list of commands that jump to a subroutine and which one they're going to, and a dictionary of subroutines and their positions.
    private string[] subroutineNames = [];
    private List<Tuple<int, string>> subroutineCalls = [];
    private Dictionary<string, Tuple<int, int>> subroutineData = new Dictionary<string, Tuple<int, int>>();
    private enum Jump : byte
    {
        IF,
        ELSE,
        // For makes things a lot more complicated. Maybe I'll come back to it later. It will likely involve restructuring.
        WHILE,
        SPAWN,
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
                throw new Exception("Parsing error " + error + " on line " + lineNumber + ": " + line);
            }
            if (varName == "")
            {
                Script.AddInstruction(Script.OpCode.RUN, lineNumber, expression);
            }
            else
            {
                var name = match.Groups[1].Value;
                var index = variableNames.IndexOf(name);
                if (index == -1)
                {
                    index = variableNames.Count;
                    variableNames.Add(name);
                    // Debug.WriteLine("ScriptCompiler.TryParseAssignment: New variable '" + name + "' at index " + index);
                    Script.AddInstruction(Script.OpCode.VAR, lineNumber, expression, index);
                }
                else
                {
                    Script.AddInstruction(Script.OpCode.SET, lineNumber, expression, index);
                }
            }
        }
        return match.Success;
    }

    private void ParseBasicInstruction(int lineNumber, Match match, Script.OpCode opCode)
    {
        Expression expression = new Expression();
        expression.Parse("[" + match.Groups[2].Value + "]");
        Script.AddInstruction(opCode, lineNumber, expression);
    }

    private bool TryParseCommand(string line, int lineNumber)
    {
        Expression expression;
        Match match = commandRegex.Match(line);
        if (match.Success)
        {
            var commandName = match.Groups[1].Value.ToLower();
            switch (commandName)
            {
                case "if":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value, variableNames.ToArray());
                    Script.AddInstruction(Script.OpCode.JUMP_IF, lineNumber, expression, Script.instructions.Count + 1);
                    stack.Push(new StackMemory(Jump.IF, Script.instructions.Count - 1, variableNames.Count));
                    return true;
                case "while":
                    expression = new Expression();
                    expression.Parse(match.Groups[2].Value, variableNames.ToArray());
                    Script.AddInstruction(Script.OpCode.JUMP_IF, lineNumber, expression, Script.instructions.Count + 1);
                    stack.Push(new StackMemory(Jump.WHILE, Script.instructions.Count - 1, variableNames.Count));
                    return true;
                case "spawn":
                    expression = new Expression();
                    expression.Parse("[" + match.Groups[2].Value + "]", variableNames.ToArray());
                    Script.AddInstruction(Script.OpCode.SPAWN, lineNumber, expression, 0, 1);
                    stack.Push(new StackMemory(Jump.SPAWN, Script.instructions.Count - 1, variableNames.Count));
                    return true;
                case "else":
                    var memory = stack.Pop();
                    variableNames.RemoveRange(memory.variableCount, variableNames.Count - memory.variableCount);
                    if (memory.command != Jump.IF)
                        throw new Exception("Error on line " + lineNumber + ": else command outside of if statement.");
                    // Add a JUMP, which will be redirected to the next END.
                    Script.AddInstruction(Script.OpCode.JUMP, lineNumber);
                    // Redirect the IF to point where you are now.
                    Script.instructions[memory.position].b = Script.instructions.Count;
                    // Make the next END redirect this JUMP to point there.
                    stack.Push(new StackMemory(Jump.ELSE, Script.instructions.Count - 1, variableNames.Count));
                    return true;
                case "end":
                    ParseEnd(lineNumber);
                    return true;
                case "subroutine":
                    Script.AddInstruction(Script.OpCode.JUMP, lineNumber);
                    // Split based on whitespace.
                    var parts = match.Groups[2].Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    //The first one is the name of the subroutine.
                    subroutineData.Add(parts[0], new Tuple<int, int> (Script.instructions.Count, variableNames.Count));
                    // This has a new scope, so push it to the stack.
                    stack.Push(new StackMemory(Jump.SUBROUTINE, Script.instructions.Count - 1, variableNames.Count));
                    // The rest are variables that need to be added.
                    variableNames.AddRange(parts.Skip(1));
                    GD.Print("ScriptCompiler.TryParseCommand: ", string.Join(", ", variableNames));
                    return true;
                default:
                    /*GD.Print("ScriptCompiler.TryParseCommand: Checking for a subroutine");
                    GD.Print("Command: " + commandName);
                    GD.Print("Subroutine Names: " + String.Join(", ", subroutineNames));*/
                    //It could be a subroutine.
                    if(Array.IndexOf(subroutineNames, commandName) != -1) {
                        GD.Print("ScriptCompiler.TryParseCommand: GOSUB");
                        GD.Print("ScriptCompiler.TryParseCommand: ", match.Groups[2].Value);
                        // Add it to the list of places the subroutine is being called at, so it can add the jump.
                        subroutineCalls.Add(new Tuple<int, string>(Script.instructions.Count, commandName));
                        expression = new Expression();
                        expression.Parse("[" + match.Groups[2].Value + "]", variableNames.ToArray());
                        Script.AddInstruction(Script.OpCode.GOSUB, lineNumber, expression);
                        return true;
                    }
                    return false;
            }
        }
        return false;
    }

    private void ParseEnd(int lineNumber)
    {
        var memory = stack.Pop();
        switch (memory.command)
        {
            case Jump.IF:
                Script.instructions[memory.position].b = Script.instructions.Count;
                break;
            case Jump.ELSE:
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
            case Jump.WHILE:
                Script.AddInstruction(Script.OpCode.JUMP, lineNumber, null, memory.position);
                Script.instructions[memory.position].b = Script.instructions.Count;
                break;
            case Jump.SPAWN:
                Script.AddInstruction(Script.OpCode.DIE, lineNumber, null);
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
            case Jump.SUBROUTINE:
                Script.AddInstruction(Script.OpCode.RETURN, lineNumber);
                Script.instructions[memory.position].a = Script.instructions.Count;
                break;
        }
        variableNames.RemoveRange(memory.variableCount, variableNames.Count - memory.variableCount);
    }

    private string[] FindSubroutines(string file)
    {
        var matches = subroutineRegex.Matches(file);
        var subroutineNames = new string[matches.Count];
        for (int i = 0; i < matches.Count; ++i)
        {
            subroutineNames[i] = matches[i].Groups[1].Value;
        }
        GD.Print("ScriptCompiler.FindSubroutines: " + String.Join(", ", subroutineNames));
        return subroutineNames;
    }

    private void SubroutineFinalPass()
    {
        foreach (var (index, name) in subroutineCalls) {
            (Script.instructions[index].a, Script.instructions[index].b) = subroutineData[name];
        }
    }

    public void ParseFile(string filename)
    {
        Script.Initialize();
        var text = File.ReadAllText(filename);
        subroutineNames = FindSubroutines(text);
        var lines = text.Split('\n');
        for (var lineNumber = 0; lineNumber < lines.Length; ++lineNumber)
        {
            var line = lines[lineNumber].Trim();
            //Empty line
            if (line.Length == 0) continue;
            //Comment
            else if (line.StartsWith('#')) continue;
            //Command (<Command> <Expression>, <Expression>, ...)
            else if (TryParseCommand(line, lineNumber)) continue;
            //Assignment (variableName = <Expression>)
            else if (TryParseAssignment(line, lineNumber)) continue;
        }
        SubroutineFinalPass();
        if (stack.Count > 0)
        {
            throw new Exception("Script is missing END command");
        }
    }
}
