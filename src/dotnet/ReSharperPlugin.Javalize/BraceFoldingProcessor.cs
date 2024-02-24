using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;

namespace ReSharperPlugin.Javalize;

internal class BraceFoldingProcessor : TreeNodeVisitor<FoldingHighlightingConsumer>, ICodeFoldingProcessor
{
    public bool InteriorShouldBeProcessed(ITreeNode element, FoldingHighlightingConsumer consumer) => true;
    public bool IsProcessingFinished(FoldingHighlightingConsumer consumer) => false;

    public void ProcessBeforeInterior(ITreeNode element, FoldingHighlightingConsumer consumer)
    {
        var treeNode = element as ICSharpTreeNode;
        treeNode?.Accept(this, consumer);
    }

    public void ProcessAfterInterior(ITreeNode element, FoldingHighlightingConsumer consumer)
    {
    }

    public override void VisitClassBody(IClassBody classBody, FoldingHighlightingConsumer consumer) =>
        ProcessBlock(classBody, classBody.LBrace, consumer);

    public override void VisitBlock(IBlock block, FoldingHighlightingConsumer consumer)
    {
        if (block.Parent is IBlock) return; // standalone block
        ProcessBlock(block, block.LBrace, consumer);
    }

    private void ProcessBlock(ITreeNode block, ITokenNode lbrace, FoldingHighlightingConsumer consumer)
    {
        var range = lbrace.GetDocumentRange();
        range = range.SetStartTo(block.SkipLeftWhitespaceTokens().GetDocumentRange().EndOffset);
        if (!range.IsEmpty && range.HasNewLine())
        {
            consumer.AddHigherPriorityFolding(BraceFoldingAttributes.BraceFoldingAttribute, range, " {");
        }
    }
}

[RegisterHighlighter(BraceFoldingAttribute, EffectType = EffectType.FOLDING,
    GroupId = CodeFoldingAttributes.GROUP_ID, TransmitUpdates = true)]
public static class BraceFoldingAttributes
{
    public const string BraceFoldingAttribute = "ReSharper Brace Folding";
}