parser grammar KickAssemblerParser;

options {
    tokenVocab = KickAssemblerLexer;
}

program: units EOF;
             // unit is a basic unit separated by newline or semicolon

// needs a lot of love
// zero or more units separated by either EOL os SEMICOLON
units
    : unit SEMICOLON+ units
    | unit EOL+ units
    | unit
    |
    ;

unit
    : instruction
    | label
    | directive
    | scope
    | compiler_statement
    | preprocessorDirective;

label
    : labelName COLON unit
    | labelName COLON;
    
instruction: COLON? fullOpcode argumentList?;               // pseudocommands are prefixed with :

scope
    : UNQUOTED_STRING COLON OPEN_BRACE units CLOSE_BRACE  // Function1: { ... }
    | OPEN_BRACE units CLOSE_BRACE;                       // { ... }

argumentList
	: argument (COMMA argument)*
	; 
argument
	: (PLUS | MINUS)+
	| HASH opcodeConstant
	| HASH numeric
	| OPEN_PARENS argumentList CLOSE_PARENS
	| OPEN_BRACKET argumentList CLOSE_BRACKET
	| labelOffsetReference
	| STAR expression                                       // jmp *-6
	| expression
	| STAR                                                  // jmp *
	;
labelOffsetReference
    : labelName MINUS
    | labelName PLUS;

expression
    : OPEN_PARENS expression CLOSE_PARENS
	| OPEN_BRACKET expression CLOSE_BRACKET // both () and [] are supported
	| expression binaryop expression
	| expression STAR expression
	| expression DIV expression
	| expression PLUS expression
	| expression MINUS expression
	| expression OP_INC
	| expression OP_DEC
	| PLUS expression
	| MINUS expression
	| expression compareop expression
    | expression INTERR expression COLON expression	        // ternary: true ? "hello" : "goodbye"
	| classFunction
	| function
	| numeric
	| opcodeConstant
	| color
	| boolean
	| labelName
	| STRING
	;
	
binaryop
    : BITWISE_OR
    | AMP
    | CARET
    | TILDE
    | OP_LEFT_SHIFT
    | OP_RIGHT_SHIFT;
	
assignment_expression
    : name=UNQUOTED_STRING ASSIGNMENT expression;
shorthand_assignment_expression
    : name=UNQUOTED_STRING unary_operator;
        
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
    :  print
    | printnow
    | var
    | const
    | if
    | errorif
    | eval
    | break
    | watch
    | enum
    | for
    | while
    | struct
    | define
    | functionDefine
    | macroDefine
    | pseudoCommandDefine
    | namespace
    | labelDirective
    | segment
    | segmentDef
    | segmentOut
    | plugin
    | fileDirective
    | diskDirective
    | modify
    | fileModify
    | return                            // should be really constrained to function
    | assert
    | assertError
    | pseudopc
    | zp
    ;

print: PRINT expression;
printnow: PRINTNOW expression;
forInit
    : forVar
    | assignment_expression
    ;
forVar: VAR assignment_expression;
var: DOTVAR assignment_expression;
const: CONST assignment_expression;
if: IF OPEN_PARENS expression CLOSE_PARENS unit (ELSE unit)?;
errorif: ERRORIF OPEN_PARENS expression CLOSE_PARENS COMMA STRING;
eval: EVAL evalAssignment;
evalAssignment
    : assignment_expression
    | shorthand_assignment_expression
    ;
break: BREAK STRING?;
watch: WATCH watchArguments;
watchArguments
    : expression
    | expression COMMA expression
    | expression COMMA expression? COMMA STRING;
enum
    : ENUM OPEN_BRACE enumValues? CLOSE_BRACE;
enumValues
    : enumValue (COMMA enumValue)*;
enumValue
    : UNQUOTED_STRING (ASSIGNMENT number)?;
for: FOR OPEN_PARENS forInit? SEMICOLON condition? SEMICOLON expression? CLOSE_PARENS unit?;
while: WHILE OPEN_PARENS condition CLOSE_PARENS unit;
struct: STRUCT UNQUOTED_STRING OPEN_BRACE variableList CLOSE_BRACE;
variableList
    : variable (COMMA variable)*
    |
    ;
variable: UNQUOTED_STRING;
define: variableList scope;
functionDefine: FUNCTION atName OPEN_PARENS variableList CLOSE_PARENS scope;
return: RETURN expression;
macroDefine: MACRO atName OPEN_PARENS variableList CLOSE_PARENS scope;
pseudoCommandDefine: PSEUDOCOMMAND UNQUOTED_STRING pseudoCommandDefineArguments;
pseudoCommandDefineArguments: UNQUOTED_STRING (COLON UNQUOTED_STRING)*;
namespace
    : NAMESPACE UNQUOTED_STRING scope?;
labelDirective: LABEL assignment_expression scope;
plugin: PLUGIN STRING;
segment
    : SEGMENT name=UNQUOTED_STRING parameterMap                                  // .segment Code [start=$0900]
    | SEGMENT name=UNQUOTED_STRING STRING                                        // .segment Code "My Code"
    | SEGMENT name=UNQUOTED_STRING;                                              // .segment Code
segmentDef
    : SEGMENTDEF Name=UNQUOTED_STRING parameterMap; // .segmentdef Combi1  [segments="Code, Data"]
segmentOut
    : SEGMENTOUT UNQUOTED_STRING parameterMap; // .segmentout [segments="ZeroPage_Code"]
fileDirective: FILE parameterMap;
diskDirective
    : DISK UNQUOTED_STRING? parameterMap OPEN_BRACE diskDirectiveContent CLOSE_BRACE;
diskDirectiveContent: parameterMap (COMMA parameterMap)*;

parameterMap: OPEN_BRACKET parameterMapItems* CLOSE_BRACKET;
parameterMapItems: parameterMapItem (COMMA parameterMapItem)*;
parameterMapItem: UNQUOTED_STRING ASSIGNMENT (number | STRING | boolean);

modify: MODIFY UNQUOTED_STRING OPEN_PARENS CLOSE_PARENS scope;
fileModify: FILEMODIFY UNQUOTED_STRING OPEN_PARENS argument CLOSE_PARENS;

assert: ASSERT STRING unit COMMA unit;
assertError: ASSERTERROR STRING unit;

pseudopc: numeric scope;                            // or should use number?
zp: ZP OPEN_BRACE EOL* zpArgumentList EOL* CLOSE_BRACE;
zpArgumentList: zpArgument (EOL zpArgument)*;
zpArgument: atName COLON DOTBYTE DEC_NUMBER; // DEC_NUMBER is really just 0
    
//segmentOptions: segmentOption (COMMA segmentOption)*;
//segmentOption
//    : memoryDirective
//    | segmentOptionSegments
//    | segmentOptionAlign
//    | ALLOW_OVERLAP
//    | segmentOptionDest
//    | FILL
//    | segmentOptionFillByte
//    | HIDE
//    | segmentOptionMarg
//    | segmentOptionMax
//    | segmentOptionMin
//    | segmentOptionModify
//    | segmentOptionOutBin
//    | segmentOptionOutPrg
//    | segmentOptionPrgFiles
//    | segmentOptionsidFiles
//    | segmentOptionStart
//    | segmentOptionStartAfter
//    | VIRTUAL
//    ;
//segmentOptionSegments: SEGMENTS UNQUOTED_STRING ASSIGNMENT DOUBLE_QUOTE segmentOptionSegmentsArguments DOUBLE_QUOTE;
//segmentOptionSegmentsArguments: UNQUOTED_STRING (COMMA UNQUOTED_STRING)*;
//segmentOptionAlign: ALIGN ASSIGNMENT number;
//segmentOptionDest: DEST ASSIGNMENT STRING;
//segmentOptionFillByte: FILL_BYTE ASSIGNMENT number;
//segmentOptionMarg: MARG MARG_INDEX;
//segmentOptionMax: MAX ASSIGNMENT number;
//segmentOptionMin: MIN ASSIGNMENT number;
//segmentOptionModify: MODIFY ASSIGNMENT STRING;
//segmentOptionOutBin: OUT_BIN ASSIGNMENT STRING;
//segmentOptionOutPrg: OUT_PRG ASSIGNMENT STRING;
//segmentOptionPrgFiles: PRG_FILES ASSIGNMENT DOUBLE_QUOTE segmentOptionPrgFilesArguments DOUBLE_QUOTE;
//segmentOptionPrgFilesArguments: segmentOptionPrgFilesArgument (COMMA segmentOptionPrgFilesArgument)?;
//segmentOptionPrgFilesArgument: UNQUOTED_STRING DIV fileName;
//segmentOptionsidFiles: SID_FILES ASSIGNMENT DOUBLE_QUOTE fileName DOUBLE_QUOTE;
//segmentOptionStart: START ASSIGNMENT number;
//segmentOptionStartAfter: START_AFTER ASSIGNMENT STRING;

fileName: UNQUOTED_STRING;
    
preprocessorDirective
    : (
        preprocessorDefine
        | preprocessorUndef
        | preprocessorImport
        | preprocessorImportIf
        | preprocessorImportOnce
        | preprocessorIf
    );
    
preprocessorDefine: HASHDEFINE DEFINED_TOKEN;
preprocessorUndef: HASHUNDEF UNDEFINED_TOKEN;
preprocessorImport: HASHIMPORT fileReference=STRING;
preprocessorImportIf: HASHIMPORTIF IIF_CONDITION fileReference=STRING;
preprocessorImportOnce: HASHIMPORTONCE fileReference=STRING;
preprocessorIf: HASHIF IF_CONDITION preprocessorBlock (HASHELIF IF_CONDITION preprocessorBlock)* (HASHELSE preprocessorBlock)? HASHENDIF;
preprocessorBlock: EOL+ (unit EOL+)?;
preprocessorCondition
    : OPEN_PARENS preprocessorCondition CLOSE_PARENS
    | BANG preprocessorCondition
    | preprocessorCondition OP_AND preprocessorCondition
    | preprocessorCondition OP_OR preprocessorCondition
    | preprocessorCondition OP_EQ preprocessorCondition
    | preprocessorCondition OP_NE preprocessorCondition
    | UNQUOTED_STRING;

directive
    : cpuDirective 
    | byteDirective 
    | wordDirective 
    | dwordDirective 
    | textDirective 
    | fillDirective
    | encodingDirective
    | importDataDirective
    | memoryDirective
    ;
     
memoryDirective: (OP_MULT_ASSIGNMENT | PC) number STRING? UNQUOTED_STRING?;
    
cpuDirective
    : DOTCPU (CPU6502NOILLEGALS | CPU6502 | DTV | CPU65C02);
    
byteDirective: DOTBYTE numberList;
wordDirective: DOTWORD numberList;
dwordDirective: DOTDWORD numberList;
    
textDirective: DOTTEXT STRING;
// implement label in front    
fillDirective
    : (DOTFILL | DOTFILLWORD | DOTLOHIFILL) number COMMA fillDirectiveArguments
    ;
    
fillDirectiveArguments
    : number
    | OPEN_BRACKET numberList CLOSE_BRACKET
    | fillExpression
    ;
    
fillExpression:
    ;
    
encodingDirective
    : DOTENCODING STRING
    ;
    
importDataDirective
    : DOTIMPORT (BINARY | C64 | TEXT | SOURCE) file (COMMA number (COMMA number)?)?
    ;
	
labelName
    : BANG UNQUOTED_STRING      #MultiLabel
    | BANG                      #MultiAnonymousLabel
    | atName                    #AtNameLabel
    ;               

atName                                      // @ prefixed name, used with macro, label and function names
    : AT UNQUOTED_STRING
    | UNQUOTED_STRING; 

file
    : STRING
    ;
    
numberList                                          // just pure numbers
    : number (COMMA number)*
    ;

numericList: numeric (COMMA numeric)*;              // extended numbers list

numeric                                             // extended number with chars and lohibytes 
    : CHAR                                          // char is a valid number
    | lohibyte? number;                             //| nonPrefixedHexNumber;

number: decNumber | hexNumber | binNumber; //| nonPrefixedHexNumber;
    
lohibyte: GT | LT;

decNumber: DEC_NUMBER ; 
hexNumber: HEX_NUMBER ;
binNumber: BIN_NUMBER ;      // testing

boolean: TRUE | FALSE;

opcodeExtension
    : UNQUOTED_STRING;

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

color
: BLACK
| WHITE
| RED
| CYAN
| PURPLE
| GREEN
| BLUE
| YELLOW
| ORANGE
| BROWN
| LIGHT_RED
| DARK_GRAY
| DARK_GREY
| GRAY
| GREY
| LIGHT_GREEN
| LIGHT_BLUE
| LIGHT_GRAY
| LIGHT_GREY;

opcodeConstant
: ADC_ABS_CONST
| ADC_ABSX_CONST
| ADC_ABSY_CONST
| ADC_IMM_CONST
| ADC_IZPX_CONST
| ADC_IZPY_CONST
| ADC_ZP_CONST
| ADC_ZPX_CONST
| AHX_ABSY_CONST
| AHX_IZPY_CONST
| ALR_IMM_CONST
| ANC_IMM_CONST
| ANC2_IMM_CONST
| AND_ABS_CONST
| AND_ABSX_CONST
| AND_ABSY_CONST
| AND_IMM_CONST
| AND_IZPX_CONST
| AND_IZPY_CONST
| AND_ZP_CONST
| AND_ZPX_CONST
| ANE_IMM_CONST
| ARR_IMM_CONST
| ASL_CONST
| ASL_ABS_CONST
| ASL_ABSX_CONST
| ASL_ZP_CONST
| ASL_ZPX_CONST
| ASR_IMM_CONST
| AXS_IMM_CONST
| BCC_REL_CONST
| BCS_REL_CONST
| BEQ_REL_CONST
| BIT_ABS_CONST
| BIT_ABSX_CONST
| BIT_IMM_CONST
| BIT_ZP_CONST
| BIT_ZPX_CONST
| BMI_REL_CONST
| BNE_REL_CONST
| BPL_REL_CONST
| BRA_REL_CONST
| BRK_CONST
| BVC_REL_CONST
| BVS_REL_CONST
| CLC_CONST
| CLD_CONST
| CLI_CONST
| CLV_CONST
| CMP_ABS_CONST
| CMP_ABSX_CONST
| CMP_ABSY_CONST
| CMP_IMM_CONST
| CMP_IZPX_CONST
| CMP_IZPY_CONST
| CMP_ZP_CONST
| CMP_ZPX_CONST
| CPX_ABS_CONST
| CPX_IMM_CONST
| CPX_ZP_CONST
| CPY_ABS_CONST
| CPY_IMM_CONST
| CPY_ZP_CONST
| DCM_ABS_CONST
| DCM_ABSX_CONST
| DCM_ABSY_CONST
| DCM_IZPX_CONST
| DCM_IZPY_CONST
| DCM_ZP_CONST
| DCM_ZPX_CONST
| DCP_ABS_CONST
| DCP_ABSX_CONST
| DCP_ABSY_CONST
| DCP_IZPX_CONST
| DCP_IZPY_CONST
| DCP_ZP_CONST
| DCP_ZPX_CONST
| DEC_CONST
| DEC_ABS_CONST
| DEC_ABSX_CONST
| DEC_ZP_CONST
| DEC_ZPX_CONST
| DEX_CONST
| DEY_CONST
| EOR_ABS_CONST
| EOR_ABSX_CONST
| EOR_ABSY_CONST
| EOR_IMM_CONST
| EOR_IZPX_CONST
| EOR_IZPY_CONST
| EOR_ZP_CONST
| EOR_ZPX_CONST
| INC_CONST
| INC_ABS_CONST
| INC_ABSX_CONST
| INC_ZP_CONST
| INC_ZPX_CONST
| INS_ABS_CONST
| INS_ABSX_CONST
| INS_ABSY_CONST
| INS_IZPX_CONST
| INS_IZPY_CONST
| INS_ZP_CONST
| INS_ZPX_CONST
| INX_CONST
| INY_CONST
| ISB_ABS_CONST
| ISB_ABSX_CONST
| ISB_ABSY_CONST
| ISB_IZPX_CONST
| ISB_IZPY_CONST
| ISB_ZP_CONST
| ISB_ZPX_CONST
| ISC_ABS_CONST
| ISC_ABSX_CONST
| ISC_ABSY_CONST
| ISC_IZPX_CONST
| ISC_IZPY_CONST
| ISC_ZP_CONST
| ISC_ZPX_CONST
| JMP_ABS_CONST
| JMP_IND_CONST
| JSR_ABS_CONST
| LAE_ABSY_CONST
| LAS_ABSY_CONST
| LAX_ABS_CONST
| LAX_ABSY_CONST
| LAX_IMM_CONST
| LAX_IZPX_CONST
| LAX_IZPY_CONST
| LAX_ZP_CONST
| LAX_ZPY_CONST
| LDA_ABS_CONST
| LDA_ABSX_CONST
| LDA_ABSY_CONST
| LDA_IMM_CONST
| LDA_IZPX_CONST
| LDA_IZPY_CONST
| LDA_ZP_CONST
| LDA_ZPX_CONST
| LDS_ABSY_CONST
| LDX_ABS_CONST
| LDX_ABSY_CONST
| LDX_IMM_CONST
| LDX_ZP_CONST
| LDX_ZPY_CONST
| LDY_ABS_CONST
| LDY_ABSX_CONST
| LDY_IMM_CONST
| LDY_ZP_CONST
| LDY_ZPX_CONST
| LSR_CONST
| LSR_ABS_CONST
| LSR_ABSX_CONST
| LSR_ZP_CONST
| LSR_ZPX_CONST
| LXA_ABS_CONST
| LXA_ABSY_CONST
| LXA_IMM_CONST
| LXA_IZPX_CONST
| LXA_IZPY_CONST
| LXA_ZP_CONST
| LXA_ZPY_CONST
| NOP_CONST
| NOP_ABS_CONST
| NOP_ABSX_CONST
| NOP_IMM_CONST
| NOP_ZP_CONST
| NOP_ZPX_CONST
| ORA_ABS_CONST
| ORA_ABSX_CONST
| ORA_ABSY_CONST
| ORA_IMM_CONST
| ORA_IZPX_CONST
| ORA_IZPY_CONST
| ORA_ZP_CONST
| ORA_ZPX_CONST
| PHA_CONST
| PHP_CONST
| PHX_CONST
| PHY_CONST
| PLA_CONST
| PLP_CONST
| PLX_CONST
| PLY_CONST
| RLA_ABS_CONST
| RLA_ABSX_CONST
| RLA_ABSY_CONST
| RLA_IZPX_CONST
| RLA_IZPY_CONST
| RLA_ZP_CONST
| RLA_ZPX_CONST
| RMB0_ZP_CONST
| RMB1_ZP_CONST
| RMB2_ZP_CONST
| RMB3_ZP_CONST
| RMB4_ZP_CONST
| RMB5_ZP_CONST
| RMB6_ZP_CONST
| RMB7_ZP_CONST
| ROL_CONST
| ROL_ABS_CONST
| ROL_ABSX_CONST
| ROL_ZP_CONST
| ROL_ZPX_CONST
| ROR_CONST
| ROR_ABS_CONST
| ROR_ABSX_CONST
| ROR_ZP_CONST
| ROR_ZPX_CONST
| RRA_ABS_CONST
| RRA_ABSX_CONST
| RRA_ABSY_CONST
| RRA_IZPX_CONST
| RRA_IZPY_CONST
| RRA_ZP_CONST
| RRA_ZPX_CONST
| RTI_CONST
| RTS_CONST
| SAC_IMM_CONST
| SAX_ABS_CONST
| SAX_IZPX_CONST
| SAX_ZP_CONST
| SAX_ZPY_CONST
| SBC_ABS_CONST
| SBC_ABSX_CONST
| SBC_ABSY_CONST
| SBC_IMM_CONST
| SBC_IZPX_CONST
| SBC_IZPY_CONST
| SBC_ZP_CONST
| SBC_ZPX_CONST
| SBC2_IMM_CONST
| SBX_IMM_CONST
| SEC_CONST
| SED_CONST
| SEI_CONST
| SHA_ABSY_CONST
| SHA_IZPY_CONST
| SHS_ABSY_CONST
| SHX_ABSY_CONST
| SHY_ABSX_CONST
| SIR_IMM_CONST
| SLO_ABS_CONST
| SLO_ABSX_CONST
| SLO_ABSY_CONST
| SLO_IZPX_CONST
| SLO_IZPY_CONST
| SLO_ZP_CONST
| SLO_ZPX_CONST
| SMB0_ZP_CONST
| SMB1_ZP_CONST
| SMB2_ZP_CONST
| SMB3_ZP_CONST
| SMB4_ZP_CONST
| SMB5_ZP_CONST
| SMB6_ZP_CONST
| SMB7_ZP_CONST
| SRE_ABS_CONST
| SRE_ABSX_CONST
| SRE_ABSY_CONST
| SRE_IZPX_CONST
| SRE_IZPY_CONST
| SRE_ZP_CONST
| SRE_ZPX_CONST
| STA_ABS_CONST
| STA_ABSX_CONST
| STA_ABSY_CONST
| STA_IZPX_CONST
| STA_IZPY_CONST
| STA_ZP_CONST
| STA_ZPX_CONST
| STP_CONST
| STX_ABS_CONST
| STX_ZP_CONST
| STX_ZPY_CONST
| STY_ABS_CONST
| STY_ZP_CONST
| STY_ZPX_CONST
| STZ_ABS_CONST
| STZ_ABSX_CONST
| STZ_ZP_CONST
| STZ_ZPX_CONST
| TAS_ABSY_CONST
| TAX_CONST
| TAY_CONST
| TRB_ABS_CONST
| TRB_ZP_CONST
| TSB_ABS_CONST
| TSB_ZP_CONST
| TSX_CONST
| TXA_CONST
| TXS_CONST
| TYA_CONST
| WAI_CONST
| XAA_IMM_CONST;