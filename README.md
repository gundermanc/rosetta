# Rosetta

This repo contains my ongoing side project to create a BNF grammar powered, programming language and IDE agnostic, language server
that can be used to provide baseline language functionality to a large number of programming languages.

It is somewhat inspired by 'TextMate' bundles (http://github.com/textmate/textmate) and 'VS Code Anycode' (https://github.com/microsoft/vscode-anycode)
but with slightly different goals.

The emphasis of this project is:

- Static, minimal bundles for languages. Grammars may end up being composable and/or reusable.
- Unlike TextMate, emphasis is put on classical BNF-style rules rather than RegEx. The goal is to avoid multi-hundred
  line regex, and their performance implications, common in TextMate grammars.
- Syntax and rule set should be simple, consistent, and predictable, and not subject to 'implementation-specific' bugs
  common in parsers that rely on regex, like TextMate.
- Unlike TreeSitter, configuration bundles should not require explicit recompilation, and any custom code should be optional,
  not required in the basic scenario.
- Files should be easy for a human to read and self-documenting where possible.
- Server, components, etc. should be componentized and easy to modify or rehost.
- Should eventually 'self-host' the bundle file.

# Example Configuration

Here is a (not yet functional) bundle example.

## Grammar

BNF-style grammatical productions. May require some additional way to specify top level options
for how to handle 'whitespace' characters.

```
ROOT = STATEMENT
IF_STATEMENT = IF LPAREN BOOL_EXPRESSION RPAREN STATEMENT
IF = 'if'
LPAREN = '('
RPAREN = ')'
SEMICOLON = ';'
STATEMENT = IF_STATEMENT | SEMICOLON
CONDITIONAL = 'true' | 'false'
BOOL_EXPRESSION = CONDITIONAL | AND_EXPRESSON | OR_EXPRESSION
AND_EXPRESSION = BOOL_EXPRESSION AND BOOL_EXPRESSION
OR_EXPRESSION = BOOL_EXPRESSION OR BOOL_EXPRESSION
AND = '&&'
OR = '||'
```

## Semantic Rules

Declarative rules describing how types are assigned to parsed AST nodes. The following
example describes rules for boolean expressions. Anything not explictly specified is
assumed to be impossible.

```
CONDITIONAL => T_BOOL
AND_EXPRESSION(T_BOOL, T_BOOL) => T_BOOL
OR_EXPRESSION(T_BOOL, T_BOOL) => T_BOOL
```

## Editor Features

My goal is to enable the following editor features:

- Completion
- Quick info
- Outlining
- Error Squiggles => { syntax, type check }
- Symbol search
- Go to definition
- Find all references

Note that some of these require 'semantic understanding' of the code and project structure.
This language server should provide baseline functionality only. Ambiguity and misbehaior,
is expected, the goal is just to provide rudimentary editor support.
