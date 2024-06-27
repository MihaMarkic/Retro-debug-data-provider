parser grammar KickAssemblerParser;

options {
    tokenVocab = KickAssemblerLexer;
}

program: units;
             // unit is a basic unit separated by newline or semicolon

// needs a lot of love

units: unit ((EOL | SEMICOLON) unit)*;

unit:
    | instruction
    | label
    | directive
    | scope;

label:
    labelName instruction;
    
instruction: fullOpcode argumentList?;

scope
    : UNQUOTED_STRING COLON OPEN_BRACE units CLOSE_BRACE  // Function1: { ... }
    | OPEN_BRACE units CLOSE_BRACE;                       // { ... }

argumentList
	: argument (COMMA argument)*
	; 
argument
	: (PLUS | MINUS)+
	| HASH numeric
	| OPEN_PARENS argumentList CLOSE_PARENS
	| OPEN_BRACKET argumentList CLOSE_BRACKET
	| expression
	;

expression
    : OPEN_PARENS expression CLOSE_PARENS
	| OPEN_BRACKET expression CLOSE_BRACKET // both () and [] are supported
	| expression binaryop expression
//	| expression logicalop expression
	| expression STAR expression
	| expression DIV expression
	| expression PLUS expression
	| expression MINUS expression
	| expression compareop expression
//	| (LT | GT) expression
	| classFunction
	| function
	| STRING
//	| pseudoOps
	| numeric
	| STRING 
	| boolean
//	| label 
	;
	
binaryop
    : BITWISE_OR
    | AMP
    | CARET
    | TILDE
    | OP_LEFT_SHIFT
    | OP_RIGHT_SHIFT;
	
assignment_expression
    : UNQUOTED_STRING ASSIGNMENT expression;
shorthand_assignment_expression
    : UNQUOTED_STRING unary_operator;
        
unary_operator
    : PLUS PLUS
    | MINUS MINUS
    | PLUS ASSIGNMENT
    | MINUS ASSIGNMENT
    | STAR ASSIGNMENT
    | DIV ASSIGNMENT
    ;

compareop
    : OP_EQ
    | OP_NE
    | OP_LE
    | OP_GE
    | GT
    | LT;
classFunction: STRING DOT STRING OPEN_PARENS argumentList? CLOSE_PARENS;
function: STRING OPEN_PARENS argumentList? CLOSE_PARENS;
	
condition: expression;
	
compiler_statement
    : DOT (
          print
        | printnow
        | var
        | const
        | if
        | errorif
        | eval
        | break
        | watch
        | enum
    );

print: PRINT expression;
printnow: PRINTNOW expression;
var: VAR assignment_expression;
const: CONST assignment_expression;
if: IF OPEN_PARENS expression CLOSE_PARENS compiler_statement;
errorif: ERRORIF OPEN_PARENS expression CLOSE_PARENS COMMA STRING;
eval: EVAL shorthand_assignment_expression;
break: BREAK STRING?;
watch: WATCH watchArguments;
watchArguments
    : expression
    | expression COMMA expression
    | expression COMMA expression? COMMA STRING;
enum
    : OPEN_BRACE enumValues CLOSE_BRACE;
enumValues
    : enumValue (COMMA enumValue)*;
enumValue
    : UNQUOTED_STRING (ASSIGNMENT number)?;

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
    : number (COMMA number)*
    ;

numericList: numeric (COMMA numeric)*;

numeric
    : CHAR                                          // char is a valid number
    | lohibyte? number;                             //| nonPrefixedHexNumber;

number: decNumber | hexNumber | binNumber; //| nonPrefixedHexNumber;
    
lohibyte: GT | LT;

decNumber: DEC_NUMBER ; 
hexNumber: HEX_NUMBER ;
binNumber: BIN_NUMBER ;      // testing

boolean: TRUE | FALSE;

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