lexer grammar KickAssemblerLexer;

channels {
    COMMENTS_CHANNEL,
    DIRECTIVE
}

ONLYA: 'a' ;
ABS: 'abs';

BINARY_TEXT: 'binary';
C64_TEXT: 'c64';
TEXT_TEXT: 'text';
ASCII: 'ascii';
PETSCII_MIXED: 'petscii_mixed';
PETSCII_UPPER: 'petscii_upper';
SCREENCODE_MIXED: 'screencode_mixed';
SCREENCODE_UPPER: 'screencode_upper';
ENCODING: 'encoding';
FILL: 'fill';
FILLWORD: 'fillword'; 
LOHIFILL: 'lohifill';
BYTE: 'byte' | 'by';
WORD: 'word' | 'wo';
DWORD: 'dword' | 'dw';
CPU: 'cpu';
CPU6502NOILLEGALS: '_6502NoIllegals';
CPU6502: '_6502';
DTV: 'dtv';
CPU65C02: '_65c02';

PRINT: 'print';
PRINTNOW: 'printnow';
VAR: 'var';
CONST: 'const';
IF: 'if';
ERRORIF: 'errorif';
EVAL: 'eval';

BREAK: 'break';
WATCH: 'watch';

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
fragment BIN_DIGIT: [01] ;
CHAR: '\'' . '\'' ;
STRING:  '"' .*? '"' ;
HASH: '#';
DOUBLE_QUOTE: '"';
UNQUOTED_STRING: [a-zA-Z0-9]+ ;
SYMBOL: '.'? [a-zA-Z0-9_]+ ;
SINGLE_LINE_COMMENT : '//' .*? EOL  -> channel(COMMENTS_CHANNEL);
MULTI_LINE_COMMENT  : '/*' .*? '*/' -> channel(COMMENTS_CHANNEL);

EOL: '\r\n' | '\r' | '\n' ;
WS : [ \t]+ -> skip ; // skip spaces, tabs, newlines

ADC
   : 'adc'   ;


AND
   : 'and'   ;


ASL
   : 'asl'   ;


BCC
   : 'bcc'   ;


BCS
   : 'bcs'   ;


BEQ
   : 'beq'   ;


BIT
   : 'bit'   ;


BMI
   : 'bmi'   ;


BNE
   : 'bne'   ;


BPL
   : 'bpl'   ;


BRA
   : 'bra'   ;


BRK
   : 'brk'   ;


BVC
   : 'bvc'   ;


BVS
   : 'bvs'   ;

CLC
   : 'clc'   ;


CLD
   : 'cld'   ;


CLI
   : 'cli'   ;


CLV
   : 'clv'   ;


CMP
   : 'cmp'   ;


CPX
   : 'cpx'   ;


CPY
   : 'cpy'   ;


DEC
   : 'dec'   ;


DEX
   : 'dex'   ;


DEY
   : 'dey'   ;


EOR
   : 'eor'   ;


INC
   : 'inc'   ;


INX
   : 'inx'   ;


INY
   : 'iny'   ;


JMP
   : 'jmp'   ;


JSR
   : 'jsr'   ;


LDA
   : 'lda'   ;


LDY
   : 'ldy'   ;


LDX
   : 'ldx'   ;


LSR
   : 'lsr'   ;


NOP
   : 'nop'   ;


ORA
   : 'ora'   ;


PHA
   : 'pha'   ;


PHX
   : 'phx'   ;


PHY
   : 'phy'   ;


PHP
   : 'php'   ;


PLA
   : 'pla'   ;


PLP
   : 'plp'   ;


PLY
   : 'ply'   ;


ROL
   : 'rol'   ;


ROR
   : 'ror'   ;


RTI
   : 'rti'   ;


RTS
   : 'rts'   ;


SBC
   : 'sbc'   ;


SEC
   : 'sec'   ;


SED
   : 'sed'   ;


SEI
   : 'sei'   ;


STA
   : 'sta'   ;


STX
   : 'stx'   ;


STY
   : 'sty'   ;


STZ
   : 'stz'   ;


TAX
   : 'tax'   ;


TAY
   : 'tay'   ;


TSX
   : 'tsx'   ;


TXA
   : 'txa'   ;


TXS
   : 'txs'   ;


TYA
   : 'tya'   ;
// chars
fragment A
   : ('a' | 'A')
   ;


fragment B
   : ('b' | 'B')
   ;


fragment C
   : ('c' | 'C')
   ;


fragment D
   : ('d' | 'D')
   ;


fragment E
   : ('e' | 'E')
   ;


fragment F
   : 'f'
   ;


fragment G
   : ('g' | 'G')
   ;


fragment H
   : ('h' | 'H')
   ;


fragment I
   : ('i' | 'I')
   ;


fragment J
   : ('j' | 'J')
   ;


fragment K
   : ('k' | 'K')
   ;


fragment L
   : ('l' | 'L')
   ;


fragment M
   : ('m' | 'M')
   ;


fragment N
   : ('n' | 'N')
   ;


fragment O
   : ('o' | 'O')
   ;


fragment P
   : ('p' | 'P')
   ;


fragment Q
   : ('q' | 'Q')
   ;


fragment R
   : ('r' | 'R')
   ;


fragment S
   : ('s' | 'S')
   ;


fragment T
   : ('t' | 'T')
   ;


fragment U
   : ('u' | 'U')
   ;


fragment V
   : ('v' | 'V')
   ;


fragment W
   : ('w' | 'W')
   ;


fragment X
   : ('x' | 'X')
   ;


fragment Y
   : ('y' | 'Y')
   ;

fragment Z
   : ('z' | 'Z')
   ;