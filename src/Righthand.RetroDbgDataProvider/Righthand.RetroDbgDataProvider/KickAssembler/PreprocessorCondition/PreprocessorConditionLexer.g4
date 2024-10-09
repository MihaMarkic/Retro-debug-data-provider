lexer grammar PreprocessorConditionLexer;

WS : [ \t\r\n]+ -> channel(HIDDEN) ; // skip spaces, tabs, newlines

OPEN_PARENS              : '(';
CLOSE_PARENS             : ')';

BANG                     : '!';
OP_AND                   : '&&';
OP_OR                    : '||';
OP_EQ                    : '==';
OP_NE                    : '!=';

UNQUOTED_STRING: [a-zA-Z_][a-zA-Z0-9_]*;

