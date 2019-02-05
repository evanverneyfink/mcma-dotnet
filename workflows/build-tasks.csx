#load "../build/project.csx"

public static readonly IBuildTask BuildCommonSteps = new AggregateTask(
    new BuildProject("workflows/ProcessWorkflowCompletion"),
    new BuildProject("workflows/ProcessWorkflowFailure"),
    new BuildProject("workflows/WorkflowActivityCallbackHandler")
);

public static readonly IBuildTask BuildConformWorkflow = new AggregateTask(
    new BuildProject("workflows/conform/01-ValidateWorkflowInput"),
    new BuildProject("workflows/conform/02-MoveContentToFileRepository"),
    new BuildProject("workflows/conform/03-CreateMediaAsset"),
    new BuildProject("workflows/conform/04-ExtractTechnicalMetadata"),
    new BuildProject("workflows/conform/05-RegisterTechnicalMetadata"),
    new BuildProject("workflows/conform/06-DecideTranscodeRequirements"),
    new BuildProject("workflows/conform/07a-ShortTranscode"),
    new BuildProject("workflows/conform/07b-LongTranscode"),
    new BuildProject("workflows/conform/08-RegisterProxyEssence"),
    new BuildProject("workflows/conform/09-CopyProxyToWebsiteStorage"),
    new BuildProject("workflows/conform/10-RegisterProxyWebsiteLocator"),
    new BuildProject("workflows/conform/11-StartAiWorkflow")
);

public static readonly IBuildTask BuildWorkflows = new AggregateTask(
    BuildCommonSteps,
    BuildConformWorkflow
);