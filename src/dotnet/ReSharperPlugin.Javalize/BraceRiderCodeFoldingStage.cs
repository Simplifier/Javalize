using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.Util;

namespace ReSharperPlugin.Javalize;

[DaemonStage(StagesBefore = [typeof(SmartResolverStage)])]
internal class BraceRiderCodeFoldingStage : IDaemonStage
{
    public IEnumerable<IDaemonStageProcess> CreateProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        DaemonProcessKind processKind
    ) =>
        processKind == DaemonProcessKind.VISIBLE_DOCUMENT
            ? [new BraceFoldingProcess(process, settings)]
            : EmptyArray<IDaemonStageProcess>.Instance;
}