using Microsoft.CodeAnalysis;

namespace ED.DOTS.EntitiesEvents.SourceGenerator
{
    internal class CodeBuildHelper
    {
        private readonly Compilation _compilation;
        private readonly INamedTypeSymbol _registerEventAttributeSymbol;

        public CodeBuildHelper(Compilation compilation)
        {
            _compilation = compilation;
            _registerEventAttributeSymbol = compilation.GetTypeByMetadataName("ED.DOTS.EntitiesEvents.RegisterEventAttribute");
        }

        public bool IsRegistrationAttribute(AttributeData attribute)
        {
            if (attribute?.AttributeClass == null)
                return false;

            return SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _registerEventAttributeSymbol);
        }
    }
}