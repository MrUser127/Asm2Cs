﻿namespace Asm2Cs;

public class ControlFlowGraph
{
    public List<Block> Blocks;

    public Block EntryBlock;

    public ControlFlowGraph(List<ILInstruction> instructions) => Rebuild(instructions);

    private void Rebuild(List<ILInstruction> instructions)
    {
        Blocks = new List<Block>();

        if (instructions.Count == 0)
            return;

        foreach (var instruction in instructions)
            instruction.IsBlockStart = true;

        GetBlockEdges(instructions, out var blockStarts, out var blockEnds);
        CreateBlocks(instructions);

        for (var i = 0; i < blockStarts.Count; i++)
            AddEdge(GetBlockByInstruction(blockStarts[i])!, GetBlockByInstruction(blockEnds[i])!);

        foreach (var instruction in instructions)
        {
            if (instruction.OpCode is ILOpCode.Jump or ILOpCode.ConditionalJump or ILOpCode.IndirectJump)
            {
                if (instruction.OpCode == ILOpCode.IndirectJump)
                    throw new NotImplementedException("Indirect jumps are not implemented");

                var target = GetBranchTarget(instruction);
                ((BranchTarget)instruction.Operands[0]).Block = GetBlockByInstruction(target);
            }
        }

        if (Blocks.Count <= 0) return;
        if (EntryBlock.Predecessors.Count <= 0) return;

        var newEntry = new Block();
        Blocks.Insert(0, newEntry);
        EntryBlock = newEntry;
        AddEdge(newEntry, Blocks[1]);
    }

    private void GetBlockEdges(List<ILInstruction> instructions, out List<ILInstruction> blockStarts,
        out List<ILInstruction> blockEnds)
    {
        blockStarts = new List<ILInstruction>();
        blockEnds = new List<ILInstruction>();

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            var nextIsBlockStart = i == instructions.Count - 1 || instruction.IsBlockStart;
            int currentInstructionIndex;

            switch (instruction.OpCode)
            {
                case ILOpCode.Jump:
                    blockStarts.Add(instruction);
                    blockEnds.Add(GetBranchTarget(instruction));
                    break;
                case ILOpCode.ConditionalJump:
                    blockStarts.Add(instruction);
                    currentInstructionIndex = instructions.FindIndex(instruction2 => instruction2 == instruction);
                    blockEnds.Add(instructions[currentInstructionIndex + 1]);
                    blockStarts.Add(instruction);
                    blockEnds.Add(GetBranchTarget(instruction));
                    break;
                case ILOpCode.IndirectJump:
                    throw new NotImplementedException("Indirect jumps are not implemented");
                case ILOpCode.Return:
                    break;
                default:
                    if (nextIsBlockStart)
                    {
                        blockStarts.Add(instruction);
                        currentInstructionIndex = instructions.FindIndex(instruction2 => instruction2 == instruction);
                        blockEnds.Add(instructions[currentInstructionIndex + 1]);
                    }

                    break;
            }
        }
    }

    private void CreateBlocks(List<ILInstruction> instructions)
    {
        Block block = new Block { ID = Blocks.Count };
        Blocks.Add(block);
        EntryBlock = block;

        block.AddInstruction(instructions[0]);

        for (var i = 1; i < instructions.Count; i++)
        {
            var instruction = instructions[i];

            if (instruction.IsBlockStart)
            {
                block = new Block { ID = Blocks.Count };
                Blocks.Add(block);
            }

            block.AddInstruction(instruction);
        }
    }

    private static ILInstruction GetBranchTarget(ILInstruction instruction) =>
        ((BranchTarget)instruction.Operands[0]).Instruction;

    private Block? GetBlockByInstruction(ILInstruction? instruction)
    {
        if (instruction == null)
            return null;

        foreach (var block in Blocks)
            if (block.Instructions.Any(blockInstruction => blockInstruction == instruction))
                return block;

        return null;
    }

    private static void AddEdge(Block from, Block to)
    {
        from.Successors.Add(to);
        to.Predecessors.Add(from);
    }
}
