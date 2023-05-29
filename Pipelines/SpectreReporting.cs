using Spectre.Console;
using System.Text;

namespace Pipelines;

public class SpectreReporter
{
    public String GetLineForPart(PipeReportPart part) => part switch
    {
        PipeReportBufferPart b => $" │ {Bar((Int32)b.State, 2, "gray35", "gray15")} ({1.0 * b.Content / b.Size:p})",
        PipeReportWorker w => $"{w.Name}",
        _ => "?"
    };

    public String GetProgressText(WorkerInputProgress progress)
        => progress.Processed > 0 ? $"{progress.Processed}{(progress.Total > 0 ? $" / {progress.Total}" : "")}" : "";

    public String RenderProgressBar(Int32 width, Double progress, String text)
    {
        var w = (Int32)(width * progress);

        var totalText = text.PadRight(width);

        var b = new StringBuilder();

        b.Append("[black on white]");

        b.Append(totalText[..w]);

        b.Append("[/][white on black]");

        b.Append(totalText[w..]);

        b.Append("[/]");

        return b.ToString();
    }

    String Bar(Int32 content, Int32 size, String foreground, String background)
        => $"{Bar(content, foreground)}{Bar(size - content, background)}";

    String Bar(Int32 size, String color)
        => $"[default on {color}]{new String(' ', size)}[/]";
}

public static class ConsoleExtensions
{
    public static LivePipeline ReportSpectre(this LivePipeline livePipeline, SpectreReporter reporter = null)
    {
        reporter ??= new SpectreReporter();

        var table = new Table();

        table.Border = TableBorder.None;

        table.AddColumn("");

        
        AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                while (!livePipeline.Task.IsCompleted)
                {
                    await Task.Delay(250);

                    {
                        var report = livePipeline.GetReport();

                        table.Rows.Clear();

                        if (report.Parts.FirstOrDefault() is PipeReportWorker inputReport)
                        {
                            var progressText = reporter.GetProgressText(inputReport.Progress);

                            var p = inputReport.Progress;

                            if (p.Processed > 0 && p.Total > 0)
                            {
                                var pf = 1.0 * p.Processed / p.Total;

                                var renderedProgress = reporter.RenderProgressBar(Console.WindowWidth, pf, progressText);

                                table.AddRow(renderedProgress);
                            }
                            else
                            {
                                table.AddRow(progressText);
                            }
                        }

                        foreach (var part in report.Parts)
                        {
                            table.AddRow(reporter.GetLineForPart(part));
                        }
                    }

                    ctx.Refresh();
                }
            });

        return livePipeline;
    }

}
