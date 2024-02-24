using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Logging;

namespace ReSharperPlugin.Javalize;

public class BraceFoldingProcess : IDaemonStageProcess
{
    public IDaemonProcess DaemonProcess { get; }
    public BraceFoldingProcess(IDaemonProcess process, IContextBoundSettingsStore settings)
    {
        DaemonProcess = process;
    }

    public void Execute(Action<DaemonStageResult> committer)
    {
        var psiServices = DaemonProcess.SourceFile.GetPsiServices();
        psiServices.Files.AssertAllDocumentAreCommitted();
        var languageManager = psiServices.LanguageManager;
        var psiFiles = DaemonProcess.SourceFile.GetPsiFiles<KnownLanguage>();
        var highlightingInfoList = new List<HighlightingInfo>().NotNull();
        foreach (var root in psiFiles)
        {
            var highlightingConsumer = new DefaultHighlightingConsumer(DaemonProcess.SourceFile);
            var context = new FoldingHighlightingConsumer(folding =>
            {
                Logger.Assert(folding.TextRange.IsValid(), "documentRange.IsValid()");
                var foldingHighlighting = new CodeFoldingHighlighting(
                    folding.AttributeId,
                    folding.Placeholder,
                    folding.TextRange,
                    true,
                    (int)folding.Priority
                );
                highlightingConsumer.ConsumeHighlighting(new(folding.TextRange, foldingHighlighting));
            });
            var services =
                languageManager.GetServices<BraceFoldingProcessorFactory>(root.Language);
            foreach (var processorFactory in services)
            {
                var processor = new InterruptableCodeFoldingProcessor(processorFactory.CreateProcessor());
                root.ProcessDescendants(processor, context);
            }

            highlightingInfoList.AppendRangeWithOverlappingResolve(highlightingConsumer.Highlightings.ToList());
        }

        committer(new(highlightingInfoList));
    }
}

file class InterruptableCodeFoldingProcessor : ICodeFoldingProcessor
{
    private readonly ICodeFoldingProcessor myProcessor;

    public InterruptableCodeFoldingProcessor(ICodeFoldingProcessor processor)
    {
        myProcessor = processor;
    }

    public bool InteriorShouldBeProcessed(ITreeNode element, FoldingHighlightingConsumer context)
    {
        return myProcessor.InteriorShouldBeProcessed(element, context);
    }

    public bool IsProcessingFinished(FoldingHighlightingConsumer context)
    {
        Interruption.Current.CheckAndThrow();
        return myProcessor.IsProcessingFinished(context);
    }

    public void ProcessBeforeInterior(ITreeNode element, FoldingHighlightingConsumer context)
    {
        myProcessor.ProcessBeforeInterior(element, context);
    }

    public void ProcessAfterInterior(ITreeNode element, FoldingHighlightingConsumer context)
    {
        myProcessor.ProcessAfterInterior(element, context);
    }
}

file static class Util
{
    internal static void AppendRangeWithOverlappingResolve(this IList<HighlightingInfo> result,
        IList<HighlightingInfo> range)
    {
        range.StableSort(FoldingComparer.Instance.Compare);
        Interruption.Current.CheckAndThrow();
        foreach (var highlightingInfo in range)
        {
            Interruption.Current.CheckAndThrow();
            InsertFolding(result, highlightingInfo);
        }
    }

    private static void InsertFolding(IList<HighlightingInfo> result, HighlightingInfo highlightingInfo)
    {
        var (startOffset1, endOffset1) = highlightingInfo.Highlighting.CalculateRange().TextRange;
        var num = result.Count;
        for (var index = 0; index < result.Count; ++index)
        {
            var textRange2 = result[index].Highlighting.CalculateRange().TextRange;
            var startOffset2 = textRange2.StartOffset;
            var endOffset2 = textRange2.EndOffset;
            if (startOffset2 < startOffset1)
            {
                if (startOffset1 < endOffset2 && endOffset2 < endOffset1)
                    return;
            }
            else if (startOffset2 == startOffset1)
            {
                if (endOffset2 == endOffset1)
                    return;
                if (endOffset2 > endOffset1)
                    num = Math.Min(num, index);
            }
            else
            {
                num = Math.Min(num, index);
                if (startOffset2 <= endOffset1)
                {
                    if (startOffset2 < endOffset1 && endOffset1 < endOffset2)
                        return;
                }
                else
                    break;
            }
        }

        result.Insert(num, highlightingInfo);
    }
}

file class FoldingComparer : IComparer<HighlightingInfo>
{
    public static readonly FoldingComparer Instance = new();

    public int Compare(HighlightingInfo x, HighlightingInfo y)
    {
        var highlighting1 = x?.Highlighting as CodeFoldingHighlighting;
        var highlighting2 = y?.Highlighting as CodeFoldingHighlighting;
        Assertion.Assert(highlighting1 != null && highlighting2 != null, "xFolding != null && yFolding != null");
        var num1 = highlighting2.FoldingPriority.CompareTo(highlighting1.FoldingPriority);
        if (num1 != 0)
            return num1;
        var range = x.Range;
        var num2 = range.TextRange.StartOffset;
        ref var local1 = ref num2;
        range = y.Range;
        var textRange = range.TextRange;
        var startOffset = textRange.StartOffset;
        var num3 = local1.CompareTo(startOffset);
        if (num3 != 0)
            return num3;
        range = x.Range;
        textRange = range.TextRange;
        num2 = textRange.EndOffset;
        ref var local2 = ref num2;
        range = y.Range;
        textRange = range.TextRange;
        var endOffset = textRange.EndOffset;
        return local2.CompareTo(endOffset);
    }
}