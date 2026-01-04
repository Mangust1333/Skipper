namespace Skipper.BaitCode.Objects.Instructions;

public enum OpCode
{
    // stack
    PUSH_CONST,
    POP,

    // locals
    LOAD_LOCAL,
    STORE_LOCAL,

    // arithmetic
    ADD_INT,
    SUB_INT,
    MUL_INT,
    DIV_INT,
    MOD_INT,

    // compare
    CMP_EQ,
    CMP_NE,
    CMP_LT,
    CMP_GT,
    CMP_LE,
    CMP_GE,

    // logic
    AND,
    OR,
    NOT,

    // control
    JUMP,
    JUMP_IF_FALSE,

    // calls
    CALL_FUNCTION,
    CALL_METHOD,

    // objects
    NEW_OBJECT,
    LOAD_FIELD,
    STORE_FIELD,

    // arrays
    NEW_ARRAY,
    LOAD_ELEMENT,
    STORE_ELEMENT,

    RETURN
}