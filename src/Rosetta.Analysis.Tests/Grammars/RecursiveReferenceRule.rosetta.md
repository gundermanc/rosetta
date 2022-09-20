# Recursive reference rule

Rosetta uses a top down parser. Left recursion in such a parser
can go on forever, causing a stack overflow, so recursive rules
must be split into two parts:

1) The non-recursive case of the rule OR-ed with the recursive rule.
2) The recursive rule with the 

```rosetta

ROOT = SELF_REFERENCING_RULE

SELF_REFERENCING_RULE = 'hello' | SELF_REFERENCING_RULE_RECURSIVE
SELF_REFERENCING_RULE_RECURSIVE = 'hello' SELF_REFERENCING_RULE
```
