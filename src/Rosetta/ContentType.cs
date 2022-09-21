namespace Rosetta.VSExtension
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Define '.rosetta' as our one known extension, for now.
    /// TODO: support arbitrary extensions.
    /// </summary>
#pragma warning disable 649
    public class ContentType
    {
        public const string Name = "Rosetta";

        [Export]
        [Name(Name)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition FooContentTypeDefinition;


        [Export]
        [FileExtension(".rosetta")]
        [ContentType(Name)]
        internal static FileExtensionToContentTypeDefinition FooFileExtensionDefinition;
    }
#pragma warning restore 649
}
