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
CLASS_DECLARATION = ACCESS_MODIFIER SEALED_MODIFIER CLASS_KEYWORD TYPEDEFINITION_EXPRESSION LCURLY_OPERATOR CLASS_BODY RCURLY_OPERATOR
```

# Class body

Defines things that can exist within classes.

```rosetta
CLASS_BODY = '' | FUNCTION_DECLARATION
```

# Function declaration

Defines the syntax for declaring the outer parts of a function.

```rosetta
FUNCTION_DECLARATION = ACCESS_MODIFIER STATIC_MODIFIER VOID_KEYWORD FUNCTIONDEFINITION_EXPRESSION LPAREN_OPERATOR RPAREN_OPERATOR LCURLY_OPERATOR RCURLY_OPERATOR
```

# Modifiers

Rules for various function and class syntax modifiers.

```rosetta
ACCESS_MODIFIER = PUBLIC_KEYWORD | PRIVATE_KEYWORD | PROTECTED_KEYWORD | INTERNAL_KEYWORD | ''
SEALED_MODIFIER = SEALED_KEYWORD | ''
STATIC_MODIFIER = STATIC_KEYWORD | ''
```

# Expressions
```rosetta

NAMESPACE_EXPRESSION = IDENTIFIER_EXPRESSION | NAMESPACE_EXPRESSION_RIGHT
NAMESPACE_EXPRESSION_RIGHT = IDENTIFIER_EXPRESSION DOT_OPERATOR IDENTIFIER_EXPRESSION

FUNCTIONDEFINITION_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'
TYPEDEFINITION_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'
TYPENAME_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'
IDENTIFIER_EXPRESSION = '^([a-z]|[A-Z])([a-z]|[A-Z]|[0-9])*'

```

# Keywords

```rosetta

USING_KEYWORD = 'using'
NAMESPACE_KEYWORD = 'namespace'
CLASS_KEYWORD = 'class'
PUBLIC_KEYWORD = 'public'
PRIVATE_KEYWORD = 'private'
PROTECTED_KEYWORD = 'protected'
INTERNAL_KEYWORD = 'internal'
SEALED_KEYWORD = 'sealed'
STATIC_KEYWORD = 'static'
VOID_KEYWORD = 'void'
```


# Operators

```rosetta

DOT_OPERATOR = '.'
LCURLY_OPERATOR = '{'
RCURLY_OPERATOR = '}'
LPAREN_OPERATOR = '('
RPAREN_OPERATOR = ')'
SEMICOLON_OPERATOR = ';'

```

# Sample file

This grammar works properly with the following file:

- Semantic colorization - colorizes keywords, type names, and functions.
- Symbol navigation - enables navigation to classes and functions by name.

```csharp
using namespace System;
using namespace System.Text;

public class Foo
{
	public static void Main()
	{
	}
}

private sealed class Bar
{

}

protected class Baz
{

}


```