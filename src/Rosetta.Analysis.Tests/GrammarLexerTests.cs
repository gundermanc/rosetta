namespace Rosetta.Analysis.Tests
{
    using System.IO;
    using System.Linq;
    using Xunit;

    public class GrammarLexerTests
    {
        [Fact]
        public void Lexer_EmptyString_NoTokens()
        {
            Assert.Empty(GrammarLexer.LexGrammar(string.Empty));
        }

        [Fact]
        public void Lexer_WhitespaceOnly_NoTokesn()
        {
            Assert.Empty(GrammarLexer.LexGrammar("   \r \n \r "));
        }

        [Fact]
        public void Lexer_AssignmentOperator_Tokenized()
        {
            var tokens = GrammarLexer.LexGrammar("=").ToList();

            Assert.Single(tokens);
            Assert.Equal("=", tokens[0]);
        }

        [Fact]
        public void Lexer_TokenName_Tokenized()
        {
            var tokens = GrammarLexer.LexGrammar("  \r  BOOL_EXPRESSION \n").ToList();

            Assert.Single(tokens);
            Assert.Equal("BOOL_EXPRESSION", tokens[0]);
        }

        [Fact]
        public void Lexer_String_Tokenized()
        {
            var tokens = GrammarLexer.LexGrammar("  \r  'true' \n").ToList();

            Assert.Equal(3, tokens.Count);
            Assert.Equal("'", tokens[0]);
            Assert.Equal("true", tokens[1]);
            Assert.Equal("'", tokens[2]);
        }
    }
}
