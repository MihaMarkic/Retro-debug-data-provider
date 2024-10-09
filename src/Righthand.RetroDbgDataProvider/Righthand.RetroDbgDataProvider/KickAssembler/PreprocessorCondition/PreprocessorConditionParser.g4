parser grammar PreprocessorConditionParser;

options {
    tokenVocab = PreprocessorConditionLexer;
}

condition
    : left=condition (OP_AND | OP_OR | OP_EQ | OP_NE) right=condition       # ConditionOperation
    | OPEN_PARENS condition CLOSE_PARENS                                    # ConditionParens
    | BANG condition                                                        # ConditionBang
    | UNQUOTED_STRING                                                       # ConditionSymbol
    ;