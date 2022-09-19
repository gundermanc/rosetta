# Rosetta test grammar

Demonstrates basic syntax highlighting in the declarative Rosetta format.

The following is the root node. It can be either nothing or a hello expression.

```rosetta
ROOT = '' | HELLO_STATEMENT
```

## Hello expressions

Hello statements let a programmer say hello to things.

```rosetta
HELLO_STATEMENT = 'Hello' THING
```

## Things

Things are entities that you can operate on.

```rosetta
THING = 'World' | 'Universe' | 'Friend'
```
