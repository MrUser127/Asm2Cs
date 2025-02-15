﻿namespace Asm2Cs;

/// <summary>
/// Intermediate language operand.
/// </summary>
public interface IILOperand
{
    /// <summary>
    /// Type of the operand.
    /// </summary>
    public OperandType OperandType { get; }
}
