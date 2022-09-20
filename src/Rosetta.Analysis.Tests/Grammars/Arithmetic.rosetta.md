# Rosetta test grammar

Demonstrates parsing that maintains operator precedence.

### Multiplicative Expression

```rosetta
MULTIPLICATIVE_EXPRESSION = MULTIPLICATION_EXPRESSION | ADDITIVE_EXPRESSION
MULTIPLICATION_EXPRESSION = ADDITIVE_EXPRESSION MULTIPLY_OPERATOR ADDITIVE_EXPRESSION
```

### Additive Expression

```rosetta
ADDITIVE_EXPRESSION = ADDITION_EXPRESSION | NUMBER
ADDITION_EXPRESSION = NUMBER ADD_OPERATOR NUMBER
```

### Operators

Individual operator strings. We haven't implemented Regex yet, so cheat
and just take numbers as 0 or 1.

```rosetta
ADD_OPERATOR = '+'
MULTIPLY_OPERATOR = '*'
NUMBER = '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'
```
