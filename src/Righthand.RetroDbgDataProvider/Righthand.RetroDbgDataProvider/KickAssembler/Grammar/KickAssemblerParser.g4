parser grammar KickAssemblerParser;

options {
    tokenVocab = KickAssemblerLexer;
}

program: 
    unit ((EOL | SEMICOLON) unit)?;         // unit is a basic unit separated by newline or semicolon

// needs a lot of love

unit:
    | instruction
    | label
    | directive;

label:
    labelName instruction;
    
instruction: fullOpcode argumentList?;

argumentList
	: argument (COMMA argument)*
	; 
argument
	: (PLUS | MINUS)+
	| HASH number
	| OPEN_PARENS argumentList CLOSE_PARENS
	| OPEN_BRACKET argumentList CLOSE_BRACKET
	| expression
	;

expression:  
	OPEN_PARENS expression CLOSE_PARENS
//	| expression binaryop expression
//	| expression logicalop expression
	| expression STAR expression
	| expression DIV expression
	| expression PLUS expression
	| expression MINUS expression
	| (LT | GT) expression
	| classFunction
	| function
	| STRING
//	| pseudoOps
	| number
	| STRING 
//	| label 
	;
	
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