using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace orch;

internal static class Program
{
    private static bool _shouldExit = false;

    public static async Task<int> Main(string[] args)
    {
        // Setup graceful exit handler
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            _shouldExit = true;
        };

        try
        {
            var configPath = FindConfigFile();
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: config.json not found[/]");
                return 1;
            }

            var configContent = await File.ReadAllTextAsync(configPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var config = JsonSerializer.Deserialize<Dictionary<string, ProjectConfig>>(configContent, options) ?? new();

            var configDir = Path.GetDirectoryName(configPath)!;

            // Include all projects (internal with Path, external with Health only)
            var projects = config.Where(x => x.Value.Path != null || x.Value.Health != null).ToDictionary();

            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects found in config.json[/]");
                return 1;
            }

            // Display available projects
            AnsiConsole.MarkupLine("[bold cyan]Available Projects:[/]");
            var table = new Table();
            table.AddColumn("Project");
            table.AddColumn("Path");
            table.AddColumn("Build");
            table.AddColumn("Run");
            table.AddColumn("Health");

            var maxPathLength = Math.Max(30, Console.WindowWidth / 5);

            foreach (var (name, proj) in projects)
            {
                var displayPath = TruncatePath(proj.Path ?? "(external)", maxPathLength);
                table.AddRow(
                    name,
                    displayPath,
                    proj.Build != null ? "✓" : "-",
                    proj.Run != null ? "✓" : "-",
                    proj.Health != null ? "✓" : "-"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Select projects to run
            var selectedProjects = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select projects to [green]build and run[/]:")
                    .NotRequired()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more projects)[/]")
                    .InstructionsText("[grey](Press [blue]<spacebar>[/] to toggle a project, [green]<enter>[/] to accept, [red]<ctrl+c>[/] to exit)[/]")
                    .AddChoices(projects.Keys)
            );

            if (_shouldExit)
            {
                AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
                return 0;
            }

            if (selectedProjects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects selected.[/]");
                return 0;
            }

            var runner = new ProjectRunner();

            // Resolve paths relative to config directory
            var resolvedProjects = projects
                .Where(x => selectedProjects.Contains(x.Key))
                .ToDictionary(
                    x => x.Key,
                    x => new ProjectConfig
                    {
                        Path = x.Value.Path != null ? Path.GetFullPath(Path.Combine(configDir, x.Value.Path)) : null,
                        Build = x.Value.Build,
                        Run = x.Value.Run,
                        Url = x.Value.Url,
                        Health = x.Value.Health
                    }
                );

            // Build phase
            AnsiConsole.MarkupLine("\n[bold cyan]Starting build phase...[/]");
            AnsiConsole.MarkupLine("[grey](Press [red]<ctrl+c>[/] to cancel and skip to next phase)[/]");
            var buildResults = new Dictionary<string, bool>();
            var buildProcesses = new Dictionary<string, Process>();

            var projectsWithBuild = resolvedProjects.Where(x => x.Value.Build != null).ToList();
            if (projectsWithBuild.Count > 0)
            {
                foreach (var kvp in projectsWithBuild)
                {
                    if (_shouldExit)
                        break;

                    var name = kvp.Key;
                    var proj = kvp.Value;
                    AnsiConsole.MarkupLine($"[cyan]Building {name}...[/]");
                    var process = runner.StartProcess(name, proj.Path!, proj.Build!);
                    buildProcesses[name] = process;
                }

                // Wait for all builds
                foreach (var (name, process) in buildProcesses)
                {
                    if (_shouldExit)
                    {
                        AnsiConsole.MarkupLine("[yellow]Cancelling build phase...[/]");
                        KillProcessGracefully(process, name);
                        buildResults[name] = false;
                    }
                    else
                    {
                        process.WaitForExit();
                        buildResults[name] = process.ExitCode == 0;

                        if (buildResults[name])
                        {
                            AnsiConsole.MarkupLine($"[green]✓ {name} built successfully[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗ {name} build failed (exit code: {process.ExitCode})[/]");
                        }
                    }
                }

                if (_shouldExit)
                {
                    _shouldExit = false; // Reset flag for next phase
                    AnsiConsole.MarkupLine("[yellow]Skipping to run phase...[/]");
                }
                else
                {
                    // Check for failures
                    var failedBuilds = buildResults.Where(x => !x.Value).ToList();
                    if (failedBuilds.Count > 0)
                    {
                        AnsiConsole.MarkupLine($"\n[red]Build failed for: {string.Join(", ", failedBuilds.Select(x => x.Key))}[/]");
                        var choice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("What would you like to do?")
                                .AddChoices("Continue with successful tasks", "Stop")
                        );

                        if (choice == "Stop")
                        {
                            return 1;
                        }

                        // Remove failed projects from selected
                        resolvedProjects = resolvedProjects.Where(x => !failedBuilds.Any(fb => fb.Key == x.Key)).ToDictionary();
                    }
                }
            }

            // Run phase
            AnsiConsole.MarkupLine("\n[bold cyan]Starting run phase...[/]");
            var runningProcesses = new Dictionary<string, (Process? process, ProjectConfig config)>();

            // Start internal projects (those with a Run command)
            foreach (var kvp in resolvedProjects.Where(x => x.Value.Run != null))
            {
                var name = kvp.Key;
                var proj = kvp.Value;
                AnsiConsole.MarkupLine($"[cyan]Starting {name}...[/]");
                var process = runner.StartProcessInWindow(name, proj.Path!, proj.Run!);
                runningProcesses[name] = (process, proj);
            }

            // Add external projects (health monitoring only, no process)
            foreach (var kvp in resolvedProjects.Where(x => x.Value.Health != null && x.Value.Run == null))
            {
                var name = kvp.Key;
                var proj = kvp.Value;
                AnsiConsole.MarkupLine($"[cyan]Monitoring external project: {name}[/]");
                runningProcesses[name] = (null, proj); // No process, only health monitoring
            }

            AnsiConsole.MarkupLine("[green]All projects started/monitored. Monitoring health status...[/]");
            AnsiConsole.MarkupLine("[grey](Press [red]<ctrl+c>[/] to gracefully stop all projects)[/]\n");

            // Health monitoring loop
            var healthChecker = new HealthChecker();
            var delays = new[] { 3000, 5000, 10000, 30000 };
            var healthStatuses = new ConcurrentDictionary<string, bool?>();

            // Initialize health statuses
            foreach (var name in runningProcesses.Keys)
            {
                healthStatuses[name] = null;
            }

            // Background task to check health continuously
            var healthCheckTask = Task.Run(async () =>
            {
                var checkIndex = 0;
                while (runningProcesses.Any(x => x.Value.process == null || !x.Value.process.HasExited) && !_shouldExit)
                {
                    var currentDelay = delays[Math.Min(checkIndex, delays.Length - 1)];
                    checkIndex++;
                    await Task.Delay(currentDelay);

                    foreach (var (name, (process, config)) in runningProcesses)
                    {
                        // Check health for both running processes and external projects
                        if (config.Health != null && (process == null || !process.HasExited))
                        {
                            var isHealthy = await healthChecker.CheckHealth(config.Health);
                            healthStatuses[name] = isHealthy;
                        }
                    }
                }
            });

            // Render status loop
            var statusTable = BuildStatusTable(runningProcesses, healthStatuses);
            
            AnsiConsole.Live(statusTable)
                .Start(ctx =>
                {
                    while (runningProcesses.Any())
                    {
                        // Handle graceful shutdown
                        if (_shouldExit)
                        {
                            AnsiConsole.MarkupLine("\n[yellow]Gracefully stopping all projects...[/]");
                            var projectsToKill = runningProcesses.Keys.ToList();
                            foreach (var projectName in projectsToKill)
                            {
                                var (process, config) = runningProcesses[projectName];
                                if (process != null) // Only kill if it's a running process
                                {
                                    KillProcessGracefully(process, projectName);
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[cyan]✓ {projectName} is external, stopping monitoring[/]");
                                }
                                runningProcesses.Remove(projectName);
                                healthStatuses.TryRemove(projectName, out _);
                            }
                            break;
                        }

                        // Update status display
                        ctx.UpdateTarget(BuildStatusTable(runningProcesses, healthStatuses));

                        // Check if any processes have exited
                        var exitedProcesses = runningProcesses.Where(x => x.Value.process != null && x.Value.process.HasExited).Select(x => x.Key).ToList();
                        foreach (var exited in exitedProcesses)
                        {
                            var process = runningProcesses[exited].process;
                            if (process != null)
                            {
                                var exitCode = process.ExitCode;
                                AnsiConsole.MarkupLine($"[red]⚠ {exited} has exited with code {exitCode}[/]");
                            }
                            runningProcesses.Remove(exited);
                            healthStatuses.TryRemove(exited, out _);
                        }

                        if (!runningProcesses.Any())
                            break;

                        // Wait before next render (1 second)
                        Task.Delay(1000).Wait();
                    }
                });

            AnsiConsole.MarkupLine("\n[yellow]All projects have stopped.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            if (args.Contains("--verbose"))
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static Table BuildStatusTable(Dictionary<string, (Process? process, ProjectConfig config)> runningProcesses, ConcurrentDictionary<string, bool?> healthStatuses)
    {
        var table = new Table()
            .Title("[bold cyan]Running Projects[/]");
        table.AddColumn("Project");
        table.AddColumn("Status");
        table.AddColumn("Health");
        table.AddColumn("URL");

        foreach (var (name, (process, config)) in runningProcesses)
        {
            var status = process == null 
                ? "[cyan]External[/]" 
                : (process.HasExited ? "[red]Stopped[/]" : "[green]Running[/]");
            var health = "N/A";
            var url = "N/A";

            if (config.Health != null)
            {
                if (healthStatuses.TryGetValue(name, out var healthStatus))
                {
                    health = healthStatus switch
                    {
                        true => "[green]✓ Healthy[/]",
                        false => "[yellow]⚠ Unhealthy[/]",
                        null => "[cyan]⏳ Checking...[/]"
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(config.Url))
            {
                var escapedUrlText = Markup.Escape(config.Url);
                url = $"[link={config.Url}]{escapedUrlText}[/]";
            }

            table.AddRow(name, status, health, url);
        }

        return table;
    }

    private static void KillProcessGracefully(Process process, string projectName)
    {
        try
        {
            if (process.HasExited)
            {
                AnsiConsole.MarkupLine($"[cyan]✓ {projectName} already stopped[/]");
                return;
            }

            // Try to kill gracefully
            try
            {
                // Send Ctrl+C to the process group (on Windows)
                if (!process.CloseMainWindow())
                {
                    // If CloseMainWindow fails, try killing the process
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Fallback: force kill
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Last resort: try Kill without process tree
                    process.Kill();
                }
            }

            // Wait a bit for process to exit
            if (!process.WaitForExit(5000))
            {
                AnsiConsole.MarkupLine($"[yellow]⚠ {projectName} did not exit gracefully. Forcing termination...[/]");
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Failed to kill {projectName}: {ex.Message}[/]");
                    AnsiConsole.MarkupLine($"[red]Please stop the process manually (PID: {process.Id})[/]");
                    return;
                }
            }

            AnsiConsole.MarkupLine($"[cyan]✓ {projectName} stopped gracefully[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error stopping {projectName}: {ex.Message}[/]");
        }
    }

    private static string TruncatePath(string path, int maxLength)
    {
        if (path.Length <= maxLength)
            return path;

        var ellipsis = "...";
        var truncatedLength = maxLength - ellipsis.Length;
        
        if (truncatedLength <= 0)
            return path.Substring(0, Math.Min(maxLength, path.Length));

        return ellipsis + path.Substring(path.Length - truncatedLength);
    }

    private static string? FindConfigFile()
    {
        // First, try current working directory
        var cwdConfig = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        if (File.Exists(cwdConfig))
            return cwdConfig;

        // Then, search up from the executable location
        var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var currentDir = Path.GetDirectoryName(executingAssembly);

        while (!string.IsNullOrEmpty(currentDir))
        {
            var configFile = Path.Combine(currentDir, "config.json");
            if (File.Exists(configFile))
                return configFile;

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
    }
}

public class ProjectConfig
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("build")]
    public string? Build { get; set; }

    [JsonPropertyName("run")]
    public string? Run { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("health")]
    public string? Health { get; set; }
}

public class ProjectRunner
{
    public Process StartProcess(string projectName, string projectPath, string command)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                WorkingDirectory = fullPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        return process;
    }

    public Process StartProcessInWindow(string projectName, string projectPath, string command)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k cd /d {fullPath} && {command}",
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };

        process.Start();
        return process;
    }
}

public class HealthChecker
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<bool> CheckHealth(string healthUrl)
    {
        try
        {
            var response = await Client.GetAsync(healthUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
