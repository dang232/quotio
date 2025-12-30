using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;
using Quotio.Core.Interfaces;

namespace Quotio.Services.Agents;

public class AgentDetectionService : IAgentDetectionService
{
    private readonly ILogger<AgentDetectionService> _logger;

    public AgentDetectionService(ILogger<AgentDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentInfo>> DetectInstalledAgentsAsync(
        CancellationToken ct = default)
    {
        var agents = new List<AgentInfo>();
        
        foreach (AgentType type in Enum.GetValues<AgentType>())
        {
            var info = await DetectAgentAsync(type, ct);
            agents.Add(info);
        }

        return agents.AsReadOnly();
    }

    private async Task<AgentInfo> DetectAgentAsync(AgentType type, CancellationToken ct)
    {
        var binaryNames = GetBinaryNames(type);
        string? foundPath = null;
        string? version = null;

        foreach (var name in binaryNames)
        {
            foundPath = await FindBinaryInPathAsync(name, ct);
            if (foundPath != null)
            {
                version = await GetVersionAsync(foundPath, ct);
                break;
            }
        }

        var isConfigured = foundPath != null && CheckIfConfigured(type);

        return new AgentInfo(type, foundPath, version, foundPath != null, isConfigured);
    }

    private static string[] GetBinaryNames(AgentType type) => type switch
    {
        AgentType.ClaudeCode => new[] { "claude.exe", "claude.cmd", "claude" },
        AgentType.CodexCLI => new[] { "codex.exe", "codex" },
        AgentType.GeminiCLI => new[] { "gemini.exe", "gemini" },
        AgentType.AmpCLI => new[] { "amp.exe", "amp" },
        AgentType.OpenCode => new[] { "opencode.exe", "opencode", "oc.exe", "oc" },
        AgentType.FactoryDroid => new[] { "droid.exe", "droid", "factory-droid.exe", "fd.exe" },
        _ => new[] { type.ToString().ToLower() + ".exe" }
    };

    private static async Task<string?> FindBinaryInPathAsync(string binaryName, CancellationToken ct)
    {
        // Check PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            try 
            {
                var fullPath = Path.Combine(path, binaryName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            catch {}
        }

        // Check common installation locations
        var commonPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                ".local", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "AppData", "Local", "Microsoft", "WinGet", "Packages")
        };

        foreach (var basePath in commonPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            
            try
            {
                // Search up to 3 levels deep to avoid massive scans
                foreach (var file in Directory.EnumerateFiles(basePath, binaryName, SearchOption.AllDirectories))
                {
                    return file;
                }
            }
            catch {}
        }

        // Use where.exe as fallback
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = binaryName,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                
                if (process.ExitCode == 0)
                {
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var firstLine = lines.FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstLine) && File.Exists(firstLine))
                        return firstLine;
                }
            }
        }
        catch { }

        return null;
    }

    private static async Task<string?> GetVersionAsync(string binaryPath, CancellationToken ct)
    {
        try
        {
            // If it's a batch/cmd file on Windows, we might need 'cmd /c'
            var isBatch = binaryPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) 
                       || binaryPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

            var psi = new ProcessStartInfo
            {
                FileName = isBatch ? "cmd.exe" : binaryPath,
                Arguments = isBatch ? $"/c \"{binaryPath}\" --version" : "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            // Add timeout for version check
            var readTask = process.StandardOutput.ReadToEndAsync(ct);
            var waitTask = process.WaitForExitAsync(ct);
            
            if (await Task.WhenAny(waitTask, Task.Delay(2000, ct)) != waitTask)
            {
                try { process.Kill(); } catch {}
                return null;
            }

            var output = await readTask;
            var match = Regex.Match(output, @"(\d+\.\d+(?:\.\d+)?)");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool CheckIfConfigured(AgentType type)
    {
        var configPath = type.GetConfigPath();
        return !string.IsNullOrEmpty(configPath) && File.Exists(configPath);
    }
}
