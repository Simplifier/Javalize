using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.Javalize;

[Language(typeof(CSharpLanguage))]
internal class BraceFoldingProcessorFactory
{
    public ICodeFoldingProcessor CreateProcessor() => new BraceFoldingProcessor();
}