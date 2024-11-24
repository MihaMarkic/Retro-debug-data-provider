﻿using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace Righthand.RetroDbgDataProvider.KickAssembler;

public static class TokensMap
{
    public static FrozenDictionary<int, TokenType> Map { get; }

    static TokensMap()
    {
        Map = BuildMap();
    }
    static FrozenDictionary<int, TokenType> BuildMap()
    {
        var map = new Dictionary<int, TokenType>()
        {
            // manual
            { KickAssemblerLexer.DEC_NUMBER, TokenType.Number },
            { KickAssemblerLexer.HEX_NUMBER, TokenType.Number },
            { KickAssemblerLexer.BIN_NUMBER, TokenType.Number },
            { KickAssemblerLexer.STRING, TokenType.String },
            { KickAssemblerLexer.SINGLE_LINE_COMMENT, TokenType.Comment },
            { KickAssemblerLexer.MULTI_LINE_COMMENT, TokenType.Comment },
            { KickAssemblerLexer.ONLYA, TokenType.InstructionExtension },
            { KickAssemblerLexer.ABS, TokenType.InstructionExtension },
          // Instructions
          { KickAssemblerLexer.ADC, TokenType.Instruction },
          { KickAssemblerLexer.AND, TokenType.Instruction },
          { KickAssemblerLexer.ASL, TokenType.Instruction },
          { KickAssemblerLexer.BCC, TokenType.Instruction },
          { KickAssemblerLexer.BCS, TokenType.Instruction },
          { KickAssemblerLexer.BEQ, TokenType.Instruction },
          { KickAssemblerLexer.BIT, TokenType.Instruction },
          { KickAssemblerLexer.BMI, TokenType.Instruction },
          { KickAssemblerLexer.BNE, TokenType.Instruction },
          { KickAssemblerLexer.BPL, TokenType.Instruction },
          { KickAssemblerLexer.BRA, TokenType.Instruction },
          { KickAssemblerLexer.BRK, TokenType.Instruction },
          { KickAssemblerLexer.BVC, TokenType.Instruction },
          { KickAssemblerLexer.BVS, TokenType.Instruction },
          { KickAssemblerLexer.CLC, TokenType.Instruction },
          { KickAssemblerLexer.CLD, TokenType.Instruction },
          { KickAssemblerLexer.CLI, TokenType.Instruction },
          { KickAssemblerLexer.CLV, TokenType.Instruction },
          { KickAssemblerLexer.CMP, TokenType.Instruction },
          { KickAssemblerLexer.CPX, TokenType.Instruction },
          { KickAssemblerLexer.CPY, TokenType.Instruction },
          { KickAssemblerLexer.DEC, TokenType.Instruction },
          { KickAssemblerLexer.DEX, TokenType.Instruction },
          { KickAssemblerLexer.DEY, TokenType.Instruction },
          { KickAssemblerLexer.EOR, TokenType.Instruction },
          { KickAssemblerLexer.INC, TokenType.Instruction },
          { KickAssemblerLexer.INX, TokenType.Instruction },
          { KickAssemblerLexer.INY, TokenType.Instruction },
          { KickAssemblerLexer.JMP, TokenType.Instruction },
          { KickAssemblerLexer.JSR, TokenType.Instruction },
          { KickAssemblerLexer.LDA, TokenType.Instruction },
          { KickAssemblerLexer.LDY, TokenType.Instruction },
          { KickAssemblerLexer.LDX, TokenType.Instruction },
          { KickAssemblerLexer.LSR, TokenType.Instruction },
          { KickAssemblerLexer.NOP, TokenType.Instruction },
          { KickAssemblerLexer.ORA, TokenType.Instruction },
          { KickAssemblerLexer.PHA, TokenType.Instruction },
          { KickAssemblerLexer.PHX, TokenType.Instruction },
          { KickAssemblerLexer.PHY, TokenType.Instruction },
          { KickAssemblerLexer.PHP, TokenType.Instruction },
          { KickAssemblerLexer.PLA, TokenType.Instruction },
          { KickAssemblerLexer.PLP, TokenType.Instruction },
          { KickAssemblerLexer.PLY, TokenType.Instruction },
          { KickAssemblerLexer.ROL, TokenType.Instruction },
          { KickAssemblerLexer.ROR, TokenType.Instruction },
          { KickAssemblerLexer.RTI, TokenType.Instruction },
          { KickAssemblerLexer.RTS, TokenType.Instruction },
          { KickAssemblerLexer.SBC, TokenType.Instruction },
          { KickAssemblerLexer.SEC, TokenType.Instruction },
          { KickAssemblerLexer.SED, TokenType.Instruction },
          { KickAssemblerLexer.SEI, TokenType.Instruction },
          { KickAssemblerLexer.STA, TokenType.Instruction },
          { KickAssemblerLexer.STX, TokenType.Instruction },
          { KickAssemblerLexer.STY, TokenType.Instruction },
          { KickAssemblerLexer.STZ, TokenType.Instruction },
          { KickAssemblerLexer.TAX, TokenType.Instruction },
          { KickAssemblerLexer.TAY, TokenType.Instruction },
          { KickAssemblerLexer.TSX, TokenType.Instruction },
          { KickAssemblerLexer.TXA, TokenType.Instruction },
          { KickAssemblerLexer.TXS, TokenType.Instruction },
          { KickAssemblerLexer.TYA, TokenType.Instruction },
          // Brackets
          { KickAssemblerLexer.OPEN_BRACE               , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_BRACE              , TokenType.Bracket },
          { KickAssemblerLexer.OPEN_BRACKET             , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_BRACKET            , TokenType.Bracket },
          { KickAssemblerLexer.OPEN_PARENS              , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_PARENS             , TokenType.Bracket },
          // Operators
          { KickAssemblerLexer.PLUS                     , TokenType.Operator },
          { KickAssemblerLexer.MINUS                    , TokenType.Operator },
          { KickAssemblerLexer.STAR                     , TokenType.Operator },
          { KickAssemblerLexer.DIV                      , TokenType.Operator },
          { KickAssemblerLexer.PERCENT                  , TokenType.Operator },
          { KickAssemblerLexer.AMP                      , TokenType.Operator },
          { KickAssemblerLexer.BITWISE_OR               , TokenType.Operator },
          { KickAssemblerLexer.CARET                    , TokenType.Operator },
          { KickAssemblerLexer.BANG                     , TokenType.Operator },
          { KickAssemblerLexer.TILDE                    , TokenType.Operator },
          { KickAssemblerLexer.AT                       , TokenType.Operator },
          { KickAssemblerLexer.ASSIGNMENT               , TokenType.Operator },
          { KickAssemblerLexer.LT                       , TokenType.Operator },
          { KickAssemblerLexer.GT                       , TokenType.Operator },
          { KickAssemblerLexer.INTERR                   , TokenType.Operator },
          { KickAssemblerLexer.DOUBLE_COLON             , TokenType.Operator },
          { KickAssemblerLexer.OP_COALESCING            , TokenType.Operator },
          { KickAssemblerLexer.OP_INC                   , TokenType.Operator },
          { KickAssemblerLexer.OP_DEC                   , TokenType.Operator },
          { KickAssemblerLexer.OP_AND                   , TokenType.Operator },
          { KickAssemblerLexer.OP_OR                    , TokenType.Operator },
          { KickAssemblerLexer.OP_PTR                   , TokenType.Operator },
          { KickAssemblerLexer.OP_EQ                    , TokenType.Operator },
          { KickAssemblerLexer.OP_NE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_LE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_GE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_ADD_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_SUB_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_MULT_ASSIGNMENT       , TokenType.Operator },
          { KickAssemblerLexer.OP_DIV_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_MOD_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_AND_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_OR_ASSIGNMENT         , TokenType.Operator },
          { KickAssemblerLexer.OP_XOR_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_LEFT_SHIFT            , TokenType.Operator },
          { KickAssemblerLexer.OP_RIGHT_SHIFT           , TokenType.Operator },
          { KickAssemblerLexer.OP_LEFT_SHIFT_ASSIGNMENT , TokenType.Operator },
          { KickAssemblerLexer.OP_COALESCING_ASSIGNMENT , TokenType.Operator },
          { KickAssemblerLexer.OP_RANGE                 , TokenType.Operator },
          // Directives
          { KickAssemblerLexer.DOTBINARY, TokenType.Directive },
          { KickAssemblerLexer.DOTC64, TokenType.Directive },
          { KickAssemblerLexer.DOTTEXT, TokenType.Directive },
          { KickAssemblerLexer.DOTENCODING, TokenType.Directive },
          { KickAssemblerLexer.DOTFILL, TokenType.Directive },
          { KickAssemblerLexer.DOTFILLWORD, TokenType.Directive },
          { KickAssemblerLexer.DOTLOHIFILL, TokenType.Directive },
          { KickAssemblerLexer.BYTE, TokenType.Directive },
          { KickAssemblerLexer.WORD, TokenType.Directive },
          { KickAssemblerLexer.DWORD, TokenType.Directive },
          { KickAssemblerLexer.DOTCPU, TokenType.Directive },
          { KickAssemblerLexer.CPU6502NOILLEGALS, TokenType.Directive },
          { KickAssemblerLexer.CPU6502, TokenType.Directive },
          { KickAssemblerLexer.DTV, TokenType.Directive },
          { KickAssemblerLexer.CPU65C02, TokenType.Directive },
          { KickAssemblerLexer.ASSERT, TokenType.Directive },
          { KickAssemblerLexer.ASSERTERROR, TokenType.Directive },
          { KickAssemblerLexer.PRINT, TokenType.Directive },
          { KickAssemblerLexer.PRINTNOW, TokenType.Directive },
          { KickAssemblerLexer.VAR, TokenType.Directive },
          { KickAssemblerLexer.CONST, TokenType.Directive },
          { KickAssemblerLexer.IF, TokenType.Directive },
          { KickAssemblerLexer.ELSE, TokenType.Directive },
          { KickAssemblerLexer.ERRORIF, TokenType.Directive },
          { KickAssemblerLexer.EVAL, TokenType.Directive },
          { KickAssemblerLexer.FOR, TokenType.Directive },
          { KickAssemblerLexer.WHILE, TokenType.Directive },
          { KickAssemblerLexer.STRUCT, TokenType.Directive },
          { KickAssemblerLexer.DEFINE, TokenType.Directive },
          { KickAssemblerLexer.FUNCTION, TokenType.Directive },
          { KickAssemblerLexer.RETURN, TokenType.Directive },
          { KickAssemblerLexer.MACRO, TokenType.Directive },
          { KickAssemblerLexer.PSEUDOCOMMAND, TokenType.Directive },
          { KickAssemblerLexer.PSEUDOPC, TokenType.Directive },
          { KickAssemblerLexer.HASHDEFINE, TokenType.Directive },
          { KickAssemblerLexer.HASHUNDEF, TokenType.Directive },
          { KickAssemblerLexer.HASHIF, TokenType.Directive },
          { KickAssemblerLexer.HASHENDIF, TokenType.Directive },
          { KickAssemblerLexer.HASHELIF, TokenType.Directive },
          { KickAssemblerLexer.HASHELSE, TokenType.Directive },
          { KickAssemblerLexer.HASHIMPORT, TokenType.Directive },
          { KickAssemblerLexer.HASHIMPORTONCE, TokenType.Directive },
          { KickAssemblerLexer.HASHIMPORTIF, TokenType.Directive },
          { KickAssemblerLexer.NAMESPACE, TokenType.Directive },
          { KickAssemblerLexer.SEGMENT, TokenType.Directive },
          { KickAssemblerLexer.SEGMENTDEF, TokenType.Directive },
          { KickAssemblerLexer.SEGMENTOUT, TokenType.Directive },
          { KickAssemblerLexer.MODIFY, TokenType.Directive },
          { KickAssemblerLexer.FILEMODIFY, TokenType.Directive },
          { KickAssemblerLexer.PLUGIN, TokenType.Directive },
          { KickAssemblerLexer.LABEL, TokenType.Directive },
          { KickAssemblerLexer.FILE, TokenType.Directive },
          { KickAssemblerLexer.DISK, TokenType.Directive },
          { KickAssemblerLexer.PC, TokenType.Directive },
          { KickAssemblerLexer.BREAK, TokenType.Directive },
          { KickAssemblerLexer.WATCH, TokenType.Directive },
          { KickAssemblerLexer.ZP, TokenType.Directive },
          // Colors
          { KickAssemblerLexer.BLACK, TokenType.Color },
          { KickAssemblerLexer.WHITE, TokenType.Color },
          { KickAssemblerLexer.RED, TokenType.Color },
          { KickAssemblerLexer.CYAN, TokenType.Color },
          { KickAssemblerLexer.PURPLE, TokenType.Color },
          { KickAssemblerLexer.GREEN, TokenType.Color },
          { KickAssemblerLexer.BLUE, TokenType.Color },
          { KickAssemblerLexer.YELLOW, TokenType.Color },
          { KickAssemblerLexer.ORANGE, TokenType.Color },
          { KickAssemblerLexer.BROWN, TokenType.Color },
          { KickAssemblerLexer.LIGHT_RED, TokenType.Color },
          { KickAssemblerLexer.DARK_GRAY, TokenType.Color },
          { KickAssemblerLexer.DARK_GREY, TokenType.Color },
          { KickAssemblerLexer.GRAY, TokenType.Color },
          { KickAssemblerLexer.GREY, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GREEN, TokenType.Color },
          { KickAssemblerLexer.LIGHT_BLUE, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GRAY, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GREY, TokenType.Color },
        };
        return map.ToFrozenDictionary();
    }
}