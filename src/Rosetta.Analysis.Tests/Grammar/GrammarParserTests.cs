namespace Rosetta.Analysis.Tests.Grammar
{
    using System.IO;
    using System.Threading.Tasks;
    using Rosetta.Analysis.Grammar;
    using Xunit;

    public sealed class GrammarParserTests
    {
        [Fact]
        public async Task Grammar_Empty_NoRulesAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\Empty.rosetta.md");

            Assert.Empty(((ParentRule)grammar.Root).Children);
        }

        [Fact]
        public async Task Grammar_StringMatchRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\StringMatchRule.rosetta.md");

            Assert.Equal("if", ((MatchRule)grammar.Root).MatchText);
        }

        [Fact]
        public async Task Grammar_AndRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\AndRule.rosetta.md");

            Assert.Equal(3, ((ParentRule)grammar.Root).Children.Count);
            Assert.IsType<AndRule>(grammar.Root);
            Assert.Equal("and", ((MatchRule)((ParentRule)grammar.Root).Children[0]).MatchText);
            Assert.Equal("expression", ((MatchRule)((ParentRule)grammar.Root).Children[1]).MatchText);
            Assert.Equal("example", ((MatchRule)((ParentRule)grammar.Root).Children[2]).MatchText);
        }

        [Fact]
        public async Task Grammar_OrRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\OrRule.rosetta.md");

            Assert.Equal(3, ((ParentRule)grammar.Root).Children.Count);
            Assert.IsType<OrRule>(grammar.Root);
            Assert.Equal("or", ((MatchRule)((ParentRule)grammar.Root).Children[0]).MatchText);
            Assert.Equal("expression", ((MatchRule)((ParentRule)grammar.Root).Children[1]).MatchText);
            Assert.Equal("example", ((MatchRule)((ParentRule)grammar.Root).Children[2]).MatchText);
        }

        [Fact]
        public async Task Grammar_ReferencedRule_ParsedCorrectlyAsync()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\ReferenceRule.rosetta.md");

            Assert.Equal("REFERENCED_EXPRESSION", ((ReferenceRule)grammar.Root).RuleName);

            Assert.Equal(2, grammar.Rules.Count);
            Assert.Contains("ROOT", grammar.Rules.Keys);
            Assert.Contains("REFERENCED_EXPRESSION", grammar.Rules.Keys);

            Assert.Equal("I understood that reference", ((MatchRule)grammar.Rules["REFERENCED_EXPRESSION"]).MatchText);
        }
    }
}
