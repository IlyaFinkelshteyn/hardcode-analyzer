using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System.Globalization;
using Tsarev.Analyzer.Helpers;

namespace Tsarev.Analyzer.Hardcode.Url
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class UrlHardcodeAnalyzer : DiagnosticAnalyzer
  {
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      nameof(UrlHardcodeAnalyzer), 
      Title, 
      MessageFormat, 
      "Hardcode", 
      DiagnosticSeverity.Warning, 
      isEnabledByDefault: true, 
      description: Description);

    private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(Rule);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.StringLiteralExpression);
      context.RegisterSyntaxNodeAction(AnalyzeInterpolation, SyntaxKind.InterpolatedStringText);
    }

    private readonly static string[] BlackList = new[] { "http:", "https:", "ftp:", "tcp:"};

    /// <summary>
    /// List of attributes that expected to contain URLs, and this is correct.
    /// </summary>
    private readonly static string[] AttributeNameWhiteList 
      = new[] {
        "WebServiceBindingAttribute",
        "DefaultSettingValueAttribute",
        "XmlTypeAttribute",
        "SoapDocumentMethodAttribute",
        "SoapRpcMethodAttribute",
        "SoapTypeAttribute",
      };

    private static void AnalyzeLiteral(SyntaxNodeAnalysisContext context)
    {
      var node = ((LiteralExpressionSyntax)context.Node);

      if (IsArgumentOfWhiteListedAttribute(node))
      {
        return;
      }

      CheckStringValue(context, node.Token.ValueText);
    }
    private static void AnalyzeInterpolation(SyntaxNodeAnalysisContext context)
    {
      var node = ((InterpolatedStringTextSyntax)context.Node);

      if (IsArgumentOfWhiteListedAttribute(node))
      {
        return;
      }

      CheckStringValue(context, node.TextToken.ValueText);
    }

    private static bool IsArgumentOfWhiteListedAttribute(SyntaxNode node)
    {
      var argument = node.Parent as AttributeArgumentSyntax;
      var attribute = argument?.WalkToAttribute();
      return attribute != null && AttributeNameWhiteList.Contains(attribute.GetAttributeName());
    }

    private static void CheckStringValue(SyntaxNodeAnalysisContext context, string value)
    {
      var comparer = CultureInfo.InvariantCulture.CompareInfo;
      if (BlackList.Any(part => comparer.IndexOf(value, part, CompareOptions.IgnoreCase) >= 0))
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), value));
      }
    }
  }
}
