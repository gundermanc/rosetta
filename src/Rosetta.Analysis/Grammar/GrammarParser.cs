namespace Rosetta.Analysis.Grammar
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class GrammarParser
    {
        public static async Task<Grammar> ParseGrammarAsync(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var reader = new StreamReader(fileStream);

            bool inDocumentation = true;
            string? line = null;

            ParentRule? rootRule = null;
            Dictionary<string, Rule> ruleDictionary = new();

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (inDocumentation)
                {
                    if (line.StartsWith("```rosetta"))
                    {
                        inDocumentation = false;
                    }
                }
                else if (line.StartsWith("```"))
                {
                    inDocumentation = true;
                }
                else
                {
                    // We're not in documentation.
                    var rule = ParseRule(line, ruleDictionary);

                    if (rootRule is null)
                    {
                        rootRule = rule;
                    }
                    else
                    {
                        rootRule.Children.Add(rule);
                    }
                }
            }

            return new Grammar(rootRule ?? new AndRule(), ruleDictionary);
        }

        private static ParentRule ParseRule(string line, Dictionary<string, Rule> ruleDictionary)
        {
            // TODO: this could be improved to use on the fly tokenization off a stream
            // instead of string allocations.
            var tokens = GrammarLexer.LexGrammar(line).ToList();

            var children = new List<Rule>();
            bool isOrRule = false;

            // Read in the production name.
            int i = 0;
            if (tokens[i].Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new InvalidDataException("First token in grammar line must be production name");
            }

            var productionName = tokens[i];

            IncrementOrThrow(tokens, ref i);

            // Check for valid rule syntax.
            if (tokens[i] != "=")
            {
                throw new InvalidDataException("Second token in grammar line must be '='");
            }

            IncrementOrThrow(tokens, ref i);

            // Can have multiple children..
            while (i < tokens.Count)
            {
                // Check for string literal values.
                if (TryParseStringChild(tokens, ref i, out var stringRule))
                {
                    children.Add(stringRule!);
                }
                else if (tokens[i] == "|")
                {
                    // or rule.
                    isOrRule = true;
                    i++;
                }
                else if (TryParseReferenceChild(tokens, ref i, out var referenceRule))
                {
                    children.Add(referenceRule!);
                }


                // TODO: today we assume AND or OR rules. We should probably throw
                // if the user tries to mix them.
            }

            ParentRule rule = isOrRule ? new OrRule() : new AndRule();
            RegisterRule(ruleDictionary, productionName, rule);

            // TODO: without list copy.
            rule.Children.AddRange(children);

            return rule;
        }

        private static void RegisterRule(Dictionary<string, Rule> ruleDictionary, string ruleName, Rule rule)
        {
            if (ruleDictionary.ContainsKey(ruleName))
            {
                throw new InvalidDataException("Duplicate production definition");
            }

            ruleDictionary.Add(ruleName, rule);
        }

        private static bool TryParseStringChild(
            IReadOnlyList<string> tokens,
            ref int i,
            out Rule? rule)
        {
            if (tokens[i] != "'")
            {
                rule = null;
                return false;
            }

            // string value.

            IncrementOrThrow(tokens, ref i);
            IncrementOrThrow(tokens, ref i);

            if (tokens[i] != "'")
            {
                throw new InvalidDataException("Expected string terminating single quote");
            }

            IncrementOrThrow(tokens, ref i);

            rule = new MatchRule(tokens[i - 2]);
            return true;
        }

        private static bool TryParseReferenceChild(
            IReadOnlyList<string> tokens,
            ref int i,
            out Rule? rule)
        {
            if (tokens[i].Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new InvalidDataException("Expected reference name");
            }

            rule = new ReferenceRule(tokens[i]);

            IncrementOrThrow(tokens, ref i);

            return true;
        }

        private static void IncrementOrThrow(IReadOnlyList<string> tokens, ref int i)
        {
            if (i >= tokens.Count)
            {
                throw new InvalidDataException("Syntax error, expected token");
            }

            i++;
        }
    }
}
