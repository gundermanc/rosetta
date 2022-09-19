namespace Rosetta.Analysis.Tests
{
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class GrammarParserTests
    {
        [Fact]
        public async Task Grammar_Empty_NoRulesAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\Empty.rosetta.md");

            Assert.Empty(grammar.Root.Children);
        }

        [Fact]
        public async Task Grammar_StringMatchRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\StringMatchRule.rosetta.md");

            Assert.Equal(1, grammar.Root.Children.Count);
            Assert.Equal("if", ((MatchRule)grammar.Root.Children[0]).MatchText);
        }

        [Fact]
        public async Task Grammar_AndRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\AndRule.rosetta.md");

            Assert.Equal(3, grammar.Root.Children.Count);
            Assert.IsType(typeof(AndRule), grammar.Root);
            Assert.Equal("and", ((MatchRule)grammar.Root.Children[0]).MatchText);
            Assert.Equal("expression", ((MatchRule)grammar.Root.Children[1]).MatchText);
            Assert.Equal("example", ((MatchRule)grammar.Root.Children[2]).MatchText);
        }

        [Fact]
        public async Task Grammar_OrRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\OrRule.rosetta.md");

            Assert.Equal(3, grammar.Root.Children.Count);
            Assert.IsType<OrRule>(grammar.Root);
            Assert.Equal("or", ((MatchRule)grammar.Root.Children[0]).MatchText);
            Assert.Equal("expression", ((MatchRule)grammar.Root.Children[1]).MatchText);
            Assert.Equal("example", ((MatchRule)grammar.Root.Children[2]).MatchText);
        }

        [Fact]
        public async Task Grammar_ReferencedRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\ReferenceRule.rosetta.md");

            Assert.Equal(2, grammar.Root.Children.Count);
            Assert.IsType<AndRule>(grammar.Root);
            Assert.Equal("REFERENCED_EXPRESSION", ((ReferenceRule)grammar.Root.Children[0]).RuleName);
        }
    }
}
