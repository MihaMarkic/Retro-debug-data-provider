parser grammar KickAssemblerParser;

options {
    tokenVocab = KickAssemblerLexer;
}

program: 
    line*;

// needs a lot of love

line: 
    instruction
    | label
    | directive;

label:
    labelName instruction;
    
instruction: fullOpcode argumentList?;

argumentList
	: argument (COMMA argumentList)?
	; 
argument
	: (PLUS | MINUS)+
	| HASH number
	| OPEN_PARENS argumentList CLOSE_PARENS
	| OPEN_BRACKET argumentList CLOSE_BRACKET
	| expression
	;

expression:  
//	'(' expression ')'
//	| expression binaryop expression
//	| expression logicalop expression
//	| expression '*' expression
//	| expression '/' expression
//	| expression '+' expression
//	| expression '-' expression
//	| ('>' | '<') expression
//	| expression logicalop expression
//	| pseudoOps
	 number
//	| CHAR 
//	| label 
	;
	
directive
    : DOT (
        cpuDirective 
        | byteDirective 
        | wordDirective 
        | dwordDirective 
        | textDirective 
        | fillDirective
        | encodingDirective
        | importFileDirective
        | importDataDirective)
    | memoryDirective
    ;
     
memoryDirective
    : STAR ASSIGNMENT number STRING
    ;
    
cpuDirective
    : CPU (CPU6502NOILLEGALS | CPU6502 | DTV | CPU65C02);
    
byteDirective: BYTE numberList;
wordDirective: WORD numberList;
dwordDirective: DWORD numberList;
    
textDirective: TEXT_TEXT STRING;
// implement label in front    
fillDirective
    : (FILL | FILLWORD | LOHIFILL) number COMMA fillDirectiveArguments
    ;
    
fillDirectiveArguments
    : number
    | OPEN_BRACKET numberList CLOSE_BRACKET
    | fillExpression
    ;
    
fillExpression:
    ;
    
encodingDirective
    : ENCODING DOUBLE_QUOTE encodingDirectiveValue DOUBLE_QUOTE
    ;
encodingDirectiveValue
    : ASCII
    | PETSCII_MIXED
    | PETSCII_UPPER
    | SCREENCODE_MIXED
    | SCREENCODE_UPPER
    ;
    
importFileDirective
    : HASH define? STRING
    ;
    
importDataDirective
    : (BINARY_TEXT | C64_TEXT | TEXT_TEXT) file (COMMA number (COMMA number)?)?
    ;
	
labelName:
    BANG UNQUOTED_STRING
    | BANG
    | UNQUOTED_STRING;
    
define
    : UNQUOTED_STRING
    ;

file
    : STRING
    ;

numberList
    : number (COMMA numberList)?
    ;

number: decNumber | hexNumber | binNumber ; //| nonPrefixedHexNumber;

decNumber: DEC_NUMBER ; 
hexNumber: HEX_NUMBER ;
binNumber: BIN_NUMBER ;      // testing

opcodeExtension
    : ONLYA
    | ABS;

fullOpcode
    : opcode
    | opcode DOT opcodeExtension;

opcode
   : ADC
   | AND
   | ASL
   | BCC
   | BCS
   | BEQ
   | BIT
   | BMI
   | BNE
   | BPL
   | BRA
   | BRK
   | BVC
   | BVS
   | CLC
   | CLD
   | CLI
   | CLV
   | CMP
   | CPX
   | CPY
   | DEC
   | DEX
   | DEY
   | EOR
   | INC
   | INX
   | INY
   | JMP
   | JSR
   | LDA
   | LDY
   | LDX
   | LSR
   | NOP
   | ORA
   | PHA
   | PHX
   | PHY
   | PHP
   | PLA
   | PLP
   | PLY
   | ROL
   | ROR
   | RTI
   | RTS
   | SBC
   | SEC
   | SED
   | SEI
   | STA
   | STX
   | STY
   | STZ
   | TAX
   | TAY
   | TSX
   | TXA
   | TXS
   | TYA
   ;