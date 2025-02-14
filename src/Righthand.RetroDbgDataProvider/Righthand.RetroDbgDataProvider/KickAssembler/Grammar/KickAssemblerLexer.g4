lexer grammar KickAssemblerLexer;

channels {
    COMMENTS_CHANNEL,
    DIRECTIVE,
    // #if undefined stuff goes here
    IGNORED
}

WS : [ \t]+ -> channel(HIDDEN) ; // skip spaces, tabs, newlines
EOL: '\r\n' | '\r' | '\n';

HASH: '#';

HASHDEFINE
    : HASH 'define'
    ->pushMode(HASHDEFINE_MODE)
    ;

HASHUNDEF
    : HASH 'undef'
    ->pushMode(HASHUNDEF_MODE)
    ;

HASHIF
    : HASH 'if'
    -> pushMode(HASHIF_WAITSPACE_MODE)
    ;

HASHENDIF
    : HASH 'endif'
    -> PopMode, PopMode
    ;

// at this point in time, #else has to be ignored
HASHELSE
    : HASH 'else'
    -> PopMode,PushMode(IGNOREALL_WAITNEWLINE_MODE)
    ;
    
HASHELIF
    : HASH 'elif'
    -> PopMode, PushMode(IGNOREALL_CONDITION_WAITSPACE_MODE)
    ;
    
HASHIMPORT
    : HASH 'import'
    -> PushMode(IMPORT_MODE)
    ;
HASHIMPORTONCE
    : HASH 'importonce'
    {
        IsImportOnce = true;
    }
    -> PushMode(IMPORT_MODE)
    ;
HASHIMPORTIF
    : HASH 'importif'
    -> PushMode(IMPORTIF_MODE)
    ;

BINARY: 'binary';
C64: 'c64';
DOTTEXT: '.text';
TEXT: 'text';
SOURCE: 'source';
DOTENCODING: '.encoding';
DOTFILL: '.fill';
DOTFILLWORD: '.fillword'; 
DOTLOHIFILL: '.lohifill';
BYTE: 'byte' | 'by';
WORD: 'word' | 'wo';
DWORD: 'dword' | 'dw';
DOTCPU: '.cpu';
DOTBYTE: '.byte';
DOTWORD: '.word';
DOTDWORD: '.dword';

CPU6502NOILLEGALS: '_6502NoIllegals';
CPU6502: '_6502';
DTV: 'dtv';
CPU65C02: '_65c02';

ASSERT: 'assert';
ASSERTERROR: 'asserterror';
PRINT: '.print';
PRINTNOW: '.printnow';
DOTVAR: '.var';
VAR: 'var';
DOTIMPORT: '.import';
CONST: '.const';
IF: '.if';
ELSE: 'else';
ERRORIF: '.errorif';
EVAL: '.eval';
ENUM: '.enum';
FOR: '.for';
WHILE: '.while';
STRUCT: '.struct';
DEFINE: '.define';
FUNCTION: '.function';
RETURN: '.return';
MACRO: '.macro';
PSEUDOCOMMAND: '.pseudocommand';
PSEUDOPC: '.pseudopc';
NAMESPACE: '.namespace'; 
SEGMENT: '.segment';
SEGMENTDEF: '.segmentdef';
SEGMENTOUT : '.segmentout';
MODIFY: '.modify';
FILEMODIFY: '.fileModify';
//OUT_BIN: 'outBin';
//OUT_PRG: 'outPrg';
//PRG_FILES: 'prgFiles';
//SEGMENTS: 'segments';
//SID_FILES: 'sidFiles';
//START: 'start';
//START_AFTER: 'startAfter';
//VIRTUAL: 'virtual';
PLUGIN: '.plugin';
LABEL: '.label';
FILE: '.file';
DISK: '.disk';
PC: '.pc';

BREAK: '.break';
WATCH: '.watch';
ZP: '.zp';

// COLORS
BLACK: 'BLACK';
WHITE: 'WHITE';
RED: 'RED';
CYAN: 'CYAN';
PURPLE: 'PURPLE';
GREEN: 'GREEN';
BLUE: 'BLUE';
YELLOW: 'YELLOW';
ORANGE: 'ORANGE';
BROWN: 'BROWN';
LIGHT_RED: 'LIGHT_RED';
DARK_GRAY: 'DARK_GRAY';
DARK_GREY: 'DARK_GREY';
GRAY: 'GRAY';
GREY: 'GREY';
LIGHT_GREEN: 'LIGHT_GREEN';
LIGHT_BLUE: 'LIGHT_BLUE';
LIGHT_GRAY: 'LIGHT_GRAY';
LIGHT_GREY: 'LIGHT_GREY';

OPEN_BRACE               : '{' ; //{ this.OnOpenBrace(); };
CLOSE_BRACE              : '}' ; //{ this.OnCloseBrace(); };
OPEN_BRACKET             : '[';
CLOSE_BRACKET            : ']';
OPEN_PARENS              : '(';
CLOSE_PARENS             : ')';
DOT                      : '.';
COMMA                    : ',';
COLON                    : ':' ; //{ this.OnColon(); };
SEMICOLON                : ';';
PLUS                     : '+';
MINUS                    : '-';
STAR                     : '*';
DIV                      : '/';
PERCENT                  : '%';
AMP                      : '&';
BITWISE_OR               : '|';
CARET                    : '^';
BANG                     : '!';
TILDE                    : '~';
AT                       : '@';
ASSIGNMENT               : '=';
LT                       : '<';
GT                       : '>';
INTERR                   : '?';
DOUBLE_COLON             : '::';
OP_COALESCING            : '??';
OP_INC                   : '++';
OP_DEC                   : '--';
OP_AND                   : '&&';
OP_OR                    : '||';
OP_PTR                   : '->';
OP_EQ                    : '==';
OP_NE                    : '!=';
OP_LE                    : '<=';
OP_GE                    : '>=';
OP_ADD_ASSIGNMENT        : '+=';
OP_SUB_ASSIGNMENT        : '-=';
OP_MULT_ASSIGNMENT       : '*=';
OP_DIV_ASSIGNMENT        : '/=';
OP_MOD_ASSIGNMENT        : '%=';
OP_AND_ASSIGNMENT        : '&=';
OP_OR_ASSIGNMENT         : '|=';
OP_XOR_ASSIGNMENT        : '^=';
OP_LEFT_SHIFT            : '<<';
OP_RIGHT_SHIFT           : '>>';
OP_LEFT_SHIFT_ASSIGNMENT : '<<=';
OP_COALESCING_ASSIGNMENT : '??=';
OP_RANGE                 : '..';

TRUE                     : 'true';
FALSE                    : 'false';

DEC_NUMBER: DEC_DIGIT+; 
HEX_NUMBER: '$' HEX_DIGIT+ ;
BIN_NUMBER: '%' BIN_DIGIT+ ;
fragment INPUT_CHAR: ~[\r\n]; // everything except newline
fragment DEC_DIGIT: [0-9] ;
fragment HEX_DIGIT: [0-9a-fA-F] ;
fragment BIN_DIGIT: '0' | '1';
CHAR: '\'' . '\'' ;
STRING:  '"' ('\\"' | ~[\n\r"])* '"' ;
OPEN_STRING: '"' ~[\n\r"]* ;
//SYMBOL: '.'? [a-zA-Z0-9_]+ ;
SINGLE_LINE_COMMENT : '//' .*? EOL  -> channel(COMMENTS_CHANNEL);
MULTI_LINE_COMMENT  : '/*' .*? '*/' -> channel(COMMENTS_CHANNEL);

// OpCodes
ADC: 'adc';
AND: 'and';
ASL: 'asl';
BCC: 'bcc';
BCS: 'bcs';
BEQ: 'beq';
BIT: 'bit';
BMI: 'bmi';
BNE: 'bne';
BPL: 'bpl';
BRA: 'bra';
BRK: 'brk';
BVC: 'bvc';
BVS: 'bvs';
CLC: 'clc';
CLD: 'cld';
CLI: 'cli';
CLV: 'clv';
CMP: 'cmp';
CPX: 'cpx';
CPY: 'cpy';
DEC: 'dec';
DEX: 'dex';
DEY: 'dey';
EOR: 'eor';
INC: 'inc';
INX: 'inx';
INY: 'iny';
JMP: 'jmp';
JSR: 'jsr';
LDA: 'lda';
LDY: 'ldy';
LDX: 'ldx';
LSR: 'lsr';
NOP: 'nop';
ORA: 'ora';
PHA: 'pha';
PHX: 'phx';
PHY: 'phy';
PHP: 'php';
PLA: 'pla';
PLP: 'plp';
PLY: 'ply';
ROL: 'rol';
ROR: 'ror';
RTI: 'rti';
RTS: 'rts';
SBC: 'sbc';
SEC: 'sec';
SED: 'sed';
SEI: 'sei';
STA: 'sta';
STX: 'stx';
STY: 'sty';
STZ: 'stz';
TAX: 'tax';
TAY: 'tay';
TSX: 'tsx';
TXA: 'txa';
TXS: 'txs';
TYA: 'tya';

// OpCodes constants
ADC_ABS_CONST: 'ADC_ABS';
ADC_ABSX_CONST: 'ADC_ABSX';
ADC_ABSY_CONST: 'ADC_ABSY';
ADC_IMM_CONST: 'ADC_IMM';
ADC_IZPX_CONST: 'ADC_IZPX';
ADC_IZPY_CONST: 'ADC_IZPY';
ADC_ZP_CONST: 'ADC_ZP';
ADC_ZPX_CONST: 'ADC_ZPX';
AHX_ABSY_CONST: 'AHX_ABSY';
AHX_IZPY_CONST: 'AHX_IZPY';
ALR_IMM_CONST: 'ALR_IMM';
ANC_IMM_CONST: 'ANC_IMM';
ANC2_IMM_CONST: 'ANC2_IMM';
AND_ABS_CONST: 'AND_ABS';
AND_ABSX_CONST: 'AND_ABSX';
AND_ABSY_CONST: 'AND_ABSY';
AND_IMM_CONST: 'AND_IMM';
AND_IZPX_CONST: 'AND_IZPX';
AND_IZPY_CONST: 'AND_IZPY';
AND_ZP_CONST: 'AND_ZP';
AND_ZPX_CONST: 'AND_ZPX';
ANE_IMM_CONST: 'ANE_IMM';
ARR_IMM_CONST: 'ARR_IMM';
ASL_CONST: 'ASL';
ASL_ABS_CONST: 'ASL_ABS';
ASL_ABSX_CONST: 'ASL_ABSX';
ASL_ZP_CONST: 'ASL_ZP';
ASL_ZPX_CONST: 'ASL_ZPX';
ASR_IMM_CONST: 'ASR_IMM';
AXS_IMM_CONST: 'AXS_IMM';
BCC_REL_CONST: 'BCC_REL';
BCS_REL_CONST: 'BCS_REL';
BEQ_REL_CONST: 'BEQ_REL';
BIT_ABS_CONST: 'BIT_ABS';
BIT_ABSX_CONST: 'BIT_ABSX';
BIT_IMM_CONST: 'BIT_IMM';
BIT_ZP_CONST: 'BIT_ZP';
BIT_ZPX_CONST: 'BIT_ZPX';
BMI_REL_CONST: 'BMI_REL';
BNE_REL_CONST: 'BNE_REL';
BPL_REL_CONST: 'BPL_REL';
BRA_REL_CONST: 'BRA_REL';
BRK_CONST: 'BRK';
BVC_REL_CONST: 'BVC_REL';
BVS_REL_CONST: 'BVS_REL';
CLC_CONST: 'CLC';
CLD_CONST: 'CLD';
CLI_CONST: 'CLI';
CLV_CONST: 'CLV';
CMP_ABS_CONST: 'CMP_ABS';
CMP_ABSX_CONST: 'CMP_ABSX';
CMP_ABSY_CONST: 'CMP_ABSY';
CMP_IMM_CONST: 'CMP_IMM';
CMP_IZPX_CONST: 'CMP_IZPX';
CMP_IZPY_CONST: 'CMP_IZPY';
CMP_ZP_CONST: 'CMP_ZP';
CMP_ZPX_CONST: 'CMP_ZPX';
CPX_ABS_CONST: 'CPX_ABS';
CPX_IMM_CONST: 'CPX_IMM';
CPX_ZP_CONST: 'CPX_ZP';
CPY_ABS_CONST: 'CPY_ABS';
CPY_IMM_CONST: 'CPY_IMM';
CPY_ZP_CONST: 'CPY_ZP';
DCM_ABS_CONST: 'DCM_ABS';
DCM_ABSX_CONST: 'DCM_ABSX';
DCM_ABSY_CONST: 'DCM_ABSY';
DCM_IZPX_CONST: 'DCM_IZPX';
DCM_IZPY_CONST: 'DCM_IZPY';
DCM_ZP_CONST: 'DCM_ZP';
DCM_ZPX_CONST: 'DCM_ZPX';
DCP_ABS_CONST: 'DCP_ABS';
DCP_ABSX_CONST: 'DCP_ABSX';
DCP_ABSY_CONST: 'DCP_ABSY';
DCP_IZPX_CONST: 'DCP_IZPX';
DCP_IZPY_CONST: 'DCP_IZPY';
DCP_ZP_CONST: 'DCP_ZP';
DCP_ZPX_CONST: 'DCP_ZPX';
DEC_CONST: 'DEC';
DEC_ABS_CONST: 'DEC_ABS';
DEC_ABSX_CONST: 'DEC_ABSX';
DEC_ZP_CONST: 'DEC_ZP';
DEC_ZPX_CONST: 'DEC_ZPX';
DEX_CONST: 'DEX';
DEY_CONST: 'DEY';
EOR_ABS_CONST: 'EOR_ABS';
EOR_ABSX_CONST: 'EOR_ABSX';
EOR_ABSY_CONST: 'EOR_ABSY';
EOR_IMM_CONST: 'EOR_IMM';
EOR_IZPX_CONST: 'EOR_IZPX';
EOR_IZPY_CONST: 'EOR_IZPY';
EOR_ZP_CONST: 'EOR_ZP';
EOR_ZPX_CONST: 'EOR_ZPX';
INC_CONST: 'INC';
INC_ABS_CONST: 'INC_ABS';
INC_ABSX_CONST: 'INC_ABSX';
INC_ZP_CONST: 'INC_ZP';
INC_ZPX_CONST: 'INC_ZPX';
INS_ABS_CONST: 'INS_ABS';
INS_ABSX_CONST: 'INS_ABSX';
INS_ABSY_CONST: 'INS_ABSY';
INS_IZPX_CONST: 'INS_IZPX';
INS_IZPY_CONST: 'INS_IZPY';
INS_ZP_CONST: 'INS_ZP';
INS_ZPX_CONST: 'INS_ZPX';
INX_CONST: 'INX';
INY_CONST: 'INY';
ISB_ABS_CONST: 'ISB_ABS';
ISB_ABSX_CONST: 'ISB_ABSX';
ISB_ABSY_CONST: 'ISB_ABSY';
ISB_IZPX_CONST: 'ISB_IZPX';
ISB_IZPY_CONST: 'ISB_IZPY';
ISB_ZP_CONST: 'ISB_ZP';
ISB_ZPX_CONST: 'ISB_ZPX';
ISC_ABS_CONST: 'ISC_ABS';
ISC_ABSX_CONST: 'ISC_ABSX';
ISC_ABSY_CONST: 'ISC_ABSY';
ISC_IZPX_CONST: 'ISC_IZPX';
ISC_IZPY_CONST: 'ISC_IZPY';
ISC_ZP_CONST: 'ISC_ZP';
ISC_ZPX_CONST: 'ISC_ZPX';
JMP_ABS_CONST: 'JMP_ABS';
JMP_IND_CONST: 'JMP_IND';
JSR_ABS_CONST: 'JSR_ABS';
LAE_ABSY_CONST: 'LAE_ABSY';
LAS_ABSY_CONST: 'LAS_ABSY';
LAX_ABS_CONST: 'LAX_ABS';
LAX_ABSY_CONST: 'LAX_ABSY';
LAX_IMM_CONST: 'LAX_IMM';
LAX_IZPX_CONST: 'LAX_IZPX';
LAX_IZPY_CONST: 'LAX_IZPY';
LAX_ZP_CONST: 'LAX_ZP';
LAX_ZPY_CONST: 'LAX_ZPY';
LDA_ABS_CONST: 'LDA_ABS';
LDA_ABSX_CONST: 'LDA_ABSX';
LDA_ABSY_CONST: 'LDA_ABSY';
LDA_IMM_CONST: 'LDA_IMM';
LDA_IZPX_CONST: 'LDA_IZPX';
LDA_IZPY_CONST: 'LDA_IZPY';
LDA_ZP_CONST: 'LDA_ZP';
LDA_ZPX_CONST: 'LDA_ZPX';
LDS_ABSY_CONST: 'LDS_ABSY';
LDX_ABS_CONST: 'LDX_ABS';
LDX_ABSY_CONST: 'LDX_ABSY';
LDX_IMM_CONST: 'LDX_IMM';
LDX_ZP_CONST: 'LDX_ZP';
LDX_ZPY_CONST: 'LDX_ZPY';
LDY_ABS_CONST: 'LDY_ABS';
LDY_ABSX_CONST: 'LDY_ABSX';
LDY_IMM_CONST: 'LDY_IMM';
LDY_ZP_CONST: 'LDY_ZP';
LDY_ZPX_CONST: 'LDY_ZPX';
LSR_CONST: 'LSR';
LSR_ABS_CONST: 'LSR_ABS';
LSR_ABSX_CONST: 'LSR_ABSX';
LSR_ZP_CONST: 'LSR_ZP';
LSR_ZPX_CONST: 'LSR_ZPX';
LXA_ABS_CONST: 'LXA_ABS';
LXA_ABSY_CONST: 'LXA_ABSY';
LXA_IMM_CONST: 'LXA_IMM';
LXA_IZPX_CONST: 'LXA_IZPX';
LXA_IZPY_CONST: 'LXA_IZPY';
LXA_ZP_CONST: 'LXA_ZP';
LXA_ZPY_CONST: 'LXA_ZPY';
NOP_CONST: 'NOP';
NOP_ABS_CONST: 'NOP_ABS';
NOP_ABSX_CONST: 'NOP_ABSX';
NOP_IMM_CONST: 'NOP_IMM';
NOP_ZP_CONST: 'NOP_ZP';
NOP_ZPX_CONST: 'NOP_ZPX';
ORA_ABS_CONST: 'ORA_ABS';
ORA_ABSX_CONST: 'ORA_ABSX';
ORA_ABSY_CONST: 'ORA_ABSY';
ORA_IMM_CONST: 'ORA_IMM';
ORA_IZPX_CONST: 'ORA_IZPX';
ORA_IZPY_CONST: 'ORA_IZPY';
ORA_ZP_CONST: 'ORA_ZP';
ORA_ZPX_CONST: 'ORA_ZPX';
PHA_CONST: 'PHA';
PHP_CONST: 'PHP';
PHX_CONST: 'PHX';
PHY_CONST: 'PHY';
PLA_CONST: 'PLA';
PLP_CONST: 'PLP';
PLX_CONST: 'PLX';
PLY_CONST: 'PLY';
RLA_ABS_CONST: 'RLA_ABS';
RLA_ABSX_CONST: 'RLA_ABSX';
RLA_ABSY_CONST: 'RLA_ABSY';
RLA_IZPX_CONST: 'RLA_IZPX';
RLA_IZPY_CONST: 'RLA_IZPY';
RLA_ZP_CONST: 'RLA_ZP';
RLA_ZPX_CONST: 'RLA_ZPX';
RMB0_ZP_CONST: 'RMB0_ZP';
RMB1_ZP_CONST: 'RMB1_ZP';
RMB2_ZP_CONST: 'RMB2_ZP';
RMB3_ZP_CONST: 'RMB3_ZP';
RMB4_ZP_CONST: 'RMB4_ZP';
RMB5_ZP_CONST: 'RMB5_ZP';
RMB6_ZP_CONST: 'RMB6_ZP';
RMB7_ZP_CONST: 'RMB7_ZP';
ROL_CONST: 'ROL';
ROL_ABS_CONST: 'ROL_ABS';
ROL_ABSX_CONST: 'ROL_ABSX';
ROL_ZP_CONST: 'ROL_ZP';
ROL_ZPX_CONST: 'ROL_ZPX';
ROR_CONST: 'ROR';
ROR_ABS_CONST: 'ROR_ABS';
ROR_ABSX_CONST: 'ROR_ABSX';
ROR_ZP_CONST: 'ROR_ZP';
ROR_ZPX_CONST: 'ROR_ZPX';
RRA_ABS_CONST: 'RRA_ABS';
RRA_ABSX_CONST: 'RRA_ABSX';
RRA_ABSY_CONST: 'RRA_ABSY';
RRA_IZPX_CONST: 'RRA_IZPX';
RRA_IZPY_CONST: 'RRA_IZPY';
RRA_ZP_CONST: 'RRA_ZP';
RRA_ZPX_CONST: 'RRA_ZPX';
RTI_CONST: 'RTI';
RTS_CONST: 'RTS';
SAC_IMM_CONST: 'SAC_IMM';
SAX_ABS_CONST: 'SAX_ABS';
SAX_IZPX_CONST: 'SAX_IZPX';
SAX_ZP_CONST: 'SAX_ZP';
SAX_ZPY_CONST: 'SAX_ZPY';
SBC_ABS_CONST: 'SBC_ABS';
SBC_ABSX_CONST: 'SBC_ABSX';
SBC_ABSY_CONST: 'SBC_ABSY';
SBC_IMM_CONST: 'SBC_IMM';
SBC_IZPX_CONST: 'SBC_IZPX';
SBC_IZPY_CONST: 'SBC_IZPY';
SBC_ZP_CONST: 'SBC_ZP';
SBC_ZPX_CONST: 'SBC_ZPX';
SBC2_IMM_CONST: 'SBC2_IMM';
SBX_IMM_CONST: 'SBX_IMM';
SEC_CONST: 'SEC';
SED_CONST: 'SED';
SEI_CONST: 'SEI';
SHA_ABSY_CONST: 'SHA_ABSY';
SHA_IZPY_CONST: 'SHA_IZPY';
SHS_ABSY_CONST: 'SHS_ABSY';
SHX_ABSY_CONST: 'SHX_ABSY';
SHY_ABSX_CONST: 'SHY_ABSX';
SIR_IMM_CONST: 'SIR_IMM';
SLO_ABS_CONST: 'SLO_ABS';
SLO_ABSX_CONST: 'SLO_ABSX';
SLO_ABSY_CONST: 'SLO_ABSY';
SLO_IZPX_CONST: 'SLO_IZPX';
SLO_IZPY_CONST: 'SLO_IZPY';
SLO_ZP_CONST: 'SLO_ZP';
SLO_ZPX_CONST: 'SLO_ZPX';
SMB0_ZP_CONST: 'SMB0_ZP';
SMB1_ZP_CONST: 'SMB1_ZP';
SMB2_ZP_CONST: 'SMB2_ZP';
SMB3_ZP_CONST: 'SMB3_ZP';
SMB4_ZP_CONST: 'SMB4_ZP';
SMB5_ZP_CONST: 'SMB5_ZP';
SMB6_ZP_CONST: 'SMB6_ZP';
SMB7_ZP_CONST: 'SMB7_ZP';
SRE_ABS_CONST: 'SRE_ABS';
SRE_ABSX_CONST: 'SRE_ABSX';
SRE_ABSY_CONST: 'SRE_ABSY';
SRE_IZPX_CONST: 'SRE_IZPX';
SRE_IZPY_CONST: 'SRE_IZPY';
SRE_ZP_CONST: 'SRE_ZP';
SRE_ZPX_CONST: 'SRE_ZPX';
STA_ABS_CONST: 'STA_ABS';
STA_ABSX_CONST: 'STA_ABSX';
STA_ABSY_CONST: 'STA_ABSY';
STA_IZPX_CONST: 'STA_IZPX';
STA_IZPY_CONST: 'STA_IZPY';
STA_ZP_CONST: 'STA_ZP';
STA_ZPX_CONST: 'STA_ZPX';
STP_CONST: 'STP';
STX_ABS_CONST: 'STX_ABS';
STX_ZP_CONST: 'STX_ZP';
STX_ZPY_CONST: 'STX_ZPY';
STY_ABS_CONST: 'STY_ABS';
STY_ZP_CONST: 'STY_ZP';
STY_ZPX_CONST: 'STY_ZPX';
STZ_ABS_CONST: 'STZ_ABS';
STZ_ABSX_CONST: 'STZ_ABSX';
STZ_ZP_CONST: 'STZ_ZP';
STZ_ZPX_CONST: 'STZ_ZPX';
TAS_ABSY_CONST: 'TAS_ABSY';
TAX_CONST: 'TAX';
TAY_CONST: 'TAY';
TRB_ABS_CONST: 'TRB_ABS';
TRB_ZP_CONST: 'TRB_ZP';
TSB_ABS_CONST: 'TSB_ABS';
TSB_ZP_CONST: 'TSB_ZP';
TSX_CONST: 'TSX';
TXA_CONST: 'TXA';
TXS_CONST: 'TXS';
TYA_CONST: 'TYA';
WAI_CONST: 'WAI';
XAA_IMM_CONST: 'XAA_IMM';

fragment INTERNAL_STRING: [a-zA-Z0-9_]+ ;

UNQUOTED_STRING: INTERNAL_STRING;
DOT_UNQUOTED_STRING: '.' INTERNAL_STRING;

mode HASHDEFINE_MODE;

DEFINED_TOKEN
    : ~[ \n\r\t]+ //add other characters as needed
    {
        DefinedSymbols.Add(Text);
    }
    ;

HD_WS
    : WS
    -> channel(HIDDEN)
    ;

HD_NEWLINE
    : EOL
    -> type(EOL),PopMode
    ;

mode HASHUNDEF_MODE;

UNDEFINED_TOKEN
    : DEFINED_TOKEN
    {
        DefinedSymbols.Remove(Text);
    }
    ;

HU_WS
    : WS
    -> channel(HIDDEN)
    ;

HU_NEWLINE
    : EOL
    -> type(EOL),PopMode
    ;

mode HASHIF_WAITSPACE_MODE;

HIWS_WS
    : WS
    -> type(WS),channel(HIDDEN),mode(HASHIF_MODE)  // here I use mode to avoid another push/pop pair
    ;

mode HASHIF_MODE;

IF_CONDITION
    : ~[\n\r]+ //add other characters as needed
    {
        if (IsDefined(Text)) {
            /*
            In this case DEFINED_TOKEN is in fact defined so we get
            back into the mode we were in before the pushMode that 
            brought us here.
            */
            PushMode(DEFAULT_MODE);
        } else {
            /*
             Push to wait for NEWLINE and only then go to IGNORE_MODE
             bacause parser would like to see EOL
            */ 
            PushMode(IGNORE_WAITNEWLINE_MODE);
        }
    }
    ;

HI_WS
    : WS
    -> channel(HIDDEN),type(WS)
    ;
    
mode IGNORE_WAITNEWLINE_MODE;

IWNL_NEWLINE
    : EOL
    -> type(EOL),mode(IGNORE_MODE)  // here I use mode to avoid another push/pop pair
    ;
    
mode DEFAULT_WAITEOL_MODE;

DWE_NEWLINE
    : EOL
    -> type(EOL),mode(DEFAULT_MODE)  // here I use mode to avoid another push/pop pair
    ;
    
DWE_WS
    : WS
    -> type(WS),channel(HIDDEN)
    ;

mode IGNORE_MODE;

I_HASHIF
    : HASHIF
    -> PushMode(HASHIF_MODE),type(HASHIF)
    ;

I_HASHENDIF
    : HASHENDIF
    ->type(HASHENDIF),PopMode,PopMode
    ;
    
I_HASHELSE
    : HASHELSE
    {
        PopMode();
        PushMode(DEFAULT_WAITEOL_MODE);
    }
    ->type(HASHELSE)
    ;
    
I_HASHELIF
    : HASHELIF
    {
        PopMode();
        PopMode();
        PushMode(HASHIF_MODE);
    }
    ->type(HASHELIF)
    ;

I_INTENTIONALLY_IGNORED
    : .+?
    ->channel(IGNORED)
    ;

mode IGNOREALL_WAITNEWLINE_MODE;

IAWNL_WS
    : WS
    -> type(WS),channel(HIDDEN)
    ;
    
IAWNL_NEWLINE
    : EOL
    -> type(EOL),mode(IGNOREALL_MODE)  // here I use mode to avoid another push/pop pair
    ;

mode IGNOREALL_CONDITION_WAITSPACE_MODE;

IA_CWS_SPACE
    : WS
    -> type(WS),channel(HIDDEN),mode(IGNOREALL_CONDITION_MODE)
    ;

mode IGNOREALL_CONDITION_MODE;

IA_C_CONDITION
    : ~[ \n\r]+
    ->type(IF_CONDITION),mode(IGNOREALL_WAITNEWLINE_MODE)
    ;

// ignores right until #endif
mode IGNOREALL_MODE;
    
IA_HASHELIF
    : HASHELIF
    -> type(HASHIF),mode(IGNOREALL_CONDITION_WAITSPACE_MODE)
    ;
    
IA_HASHELSE
    : HASHELSE
    -> type(HASHELSE),mode(IGNOREALL_WAITNEWLINE_MODE)
    ;

IA_HASHENDIF
    : HASHENDIF
    ->type(HASHENDIF),PopMode,PopMode
    ;

IA_INTENTIONALLY_IGNORED
    : ~[\n\r]+
    ->channel(IGNORED)
    ;
IA_EOL
    : EOL
    ->type(EOL),channel(IGNORED)
    ;
    
    
mode IMPORT_MODE;

REFERENCED_FILEPATH
    : STRING
    {
        AddReferencedFileInfo(TokenStartLine, TokenStartColumn, Text);
    }
    ->type(STRING),PopMode
    ;
    
IM_OPEN_STRING
    : OPEN_STRING
    -> type(OPEN_STRING)
    ;
    
IM_UNQUOTED_STRING
    : UNQUOTED_STRING
    -> type(UNQUOTED_STRING)
    ;
    
IM_DOT
    : DOT
    -> type(DOT)
    ;
    
IM_WS
    : WS
    -> type(WS),channel(HIDDEN)
    ;
    
IM_EOL
    : EOL
    ->type(EOL),channel(COMMENTS_CHANNEL),PopMode
    ;
    
mode IMPORTIF_MODE;

IIF_CONDITION
    : ~[\n\r"]+ //add other characters as needed
    {
        if (IsDefined(Text)) {
            /*
            In this case DEFINED_TOKEN is in fact defined so we get
            back into the mode we were in before the pushMode that 
            brought us here.
            */
            PushMode(IMPORT_MODE);
        } else {
            PopMode();
        }
    }
    ;

IIF_WS
    : WS
    -> type(WS),channel(HIDDEN)
    ;