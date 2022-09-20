# CSharp grammar

Pseudo-csharp language grammar demonstrating right hand recursion and code colorization.

# Usings

Currently supports one or more using statements.

```rosetta

ROOT = DECLARATIONS | BODY

BODY = USING_BLOCK DECLARATIONS

DECLARATIONS = CLASS_DECLARATION | DECLARATIONS_RIGHT
DECLARATIONS_RIGHT = CLASS_DECLARATION DECLARATIONS

USING_BLOCK = USING | USING_BLOCK_RIGHT
USING_BLOCK_RIGHT = USING USING_BLOCK

USING = USING_KEYWORD NAMESPACE_KEYWORD NAMESPACE_EXPRESSION SEMICOLON_OPERATOR

```

# Class Declaration

Defines syntax for classes.

```rosetta
CLASS_DECLARATION = CLASS_KEYWORD TYPENAME_EXPRESSION LCURLY_OPERATOR RCURLY_OPERATOR
```

# Expressions
```rosetta

NAMESPACE_EXPRESSION = IDENTIFIER_EXPRESSION | NAMESPACE_EXPRESSION_RIGHT
NAMESPACE_EXPRESSION_RIGHT = IDENTIFIER_EXPRESSION DOT_OPERATOR IDENTIFIER_EXPRESSION

TYPENAME_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'
IDENTIFIER_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'

```

# Keywords

```rosetta

USING_KEYWORD = 'using'
NAMESPACE_KEYWORD = 'namespace'
CLASS_KEYWORD = 'class'
```


# Operators

```rosetta

DOT_OPERATOR = '.'
LCURLY_OPERATOR = '{'
RCURLY_OPERATOR = '}'
SEMICOLON_OPERATOR = ';'

```

# Sample file

This grammar can successfully parse and colorize the following file.

```csharp
using namespace System;
using namespace System.Text;

class Foo
{

}

class Bar
{

}

class Baz
{

}
```