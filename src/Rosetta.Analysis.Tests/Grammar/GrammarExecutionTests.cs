namespace Rosetta.Analysis.Tests.Grammar
{
    using System.Threading.Tasks;
    using Rosetta.Analysis.Grammar;
    using Rosetta.Analysis.GrammarExecution;
    using Rosetta.Analysis.Text;
    using Xunit;

    public sealed class GrammarExecutionTests
    {
        [Fact]
        public async Task GrammarExecution_NoRules_EmptySyntaxTree()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\Empty.rosetta.md");
            var syntaxTree = GrammarExecution.Parse(grammar, new StringSnapshot(string.Empty));
        }

        [Fact]
        public async Task GrammarExecution_SimpleArithmetic_ParsesCorrectly()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\Arithmetic.rosetta.md");
            var syntaxTree = GrammarExecution.Parse(grammar, new StringSnapshot("3 + 5"));

            Assert.Equal("ADDITION_EXPRESSION", syntaxTree.Root.RuleName);
            Assert.Equal(3, syntaxTree.Root.Children.Count);

            Assert.Equal("3", syntaxTree.Root.Children[0].Text.GetText());
            Assert.Equal("NUMBER", syntaxTree.Root.Children[0].RuleName);

            Assert.Equal("+", syntaxTree.Root.Children[1].Text.GetText());
            Assert.Equal("ADD_OPERATOR", syntaxTree.Root.Children[1].RuleName);

            Assert.Equal("5", syntaxTree.Root.Children[2].Text.GetText());
            Assert.Equal("NUMBER", syntaxTree.Root.Children[2].RuleName);
        }

        [Fact]
        public async Task GrammarExecution_Arithmetic_ParsesCorrectly()
        {
            var grammar = await GrammarParser.ParseGrammarAsync(@"Grammars\Arithmetic.rosetta.md");
            var syntaxTree = GrammarExecution.Parse(grammar, new StringSnapshot("3 + 5 * 2"));

            Assert.Equal("MULTIPLICATION_EXPRESSION", syntaxTree.Root.RuleName);
            Assert.Equal(3, syntaxTree.Root.Children.Count);

            Assert.Equal("ADDITION_EXPRESSION", syntaxTree.Root.Children[0].RuleName);
            Assert.Equal(3, syntaxTree.Root.Children[0].Children.Count);

            Assert.Equal("3", syntaxTree.Root.Children[0].Children[0].Text.GetText());
            Assert.Equal("NUMBER", syntaxTree.Root.Children[0].Children[0].RuleName);

            Assert.Equal("+", syntaxTree.Root.Children[0].Children[1].Text.GetText());
            Assert.Equal("ADD_OPERATOR", syntaxTree.Root.Children[0].Children[1].RuleName);

            Assert.Equal("5", syntaxTree.Root.Children[0].Children[2].Text.GetText());
            Assert.Equal("NUMBER", syntaxTree.Root.Children[0].Children[2].RuleName);

            Assert.Equal("*", syntaxTree.Root.Children[1].Text.GetText());
            Assert.Equal("MULTIPLY_OPERATOR", syntaxTree.Root.Children[1].RuleName);

            Assert.Equal("2", syntaxTree.Root.Children[2].Text.GetText());
            Assert.Equal("NUMBER", syntaxTree.Root.Children[2].RuleName);
        }
    }
}
