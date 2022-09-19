namespace Rosetta.Analysis.Grammar
{
    using System.Collections.Generic;

    public sealed class GrammarLexer
    {
        private const string EqualsToken = "=";
        private const string PipeToken = "|";
        private const string SingleQuoteToken = "'";

        public static IEnumerable<string> LexGrammar(string grammar)
        {
            for (int i = 0; i < grammar.Length; i++)
            {
                switch (grammar[i])
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // Ignore whitespace.
                        break;

                    case '|':
                        yield return PipeToken;
                        break;

                    case '\'':
                        yield return SingleQuoteToken;

                        int stringStart = ++i;

                        // Consume up to the end of the string.
                        // TODO: support escape sequences.
                        for (;
                            i < grammar.Length &&
                            grammar[i] != '\'';
                            i++) ;

                        yield return grammar.Substring(stringStart, i - stringStart);

                        yield return SingleQuoteToken;
                        break;

                    case '=':
                        yield return EqualsToken;
                        break;

                    default:
                        int tokenNameStart = i;

                        // Consume up to the end of the token.
                        for (;
                            i < grammar.Length &&
                            (char.IsLetterOrDigit(grammar[i]) || grammar[i] == '_');
                            i++) ;

                        yield return grammar.Substring(tokenNameStart, i - tokenNameStart);
                        break;
                }
            }
        }
    }
}
