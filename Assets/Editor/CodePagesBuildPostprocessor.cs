using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal sealed class CodePagesBuildPostprocessor : IPostprocessBuildWithReport
{
    private const string AssemblyFileName = "System.Text.Encoding.CodePages.dll";
    private const long MinimumImplementationSize = 100_000;

    public int callbackOrder => int.MaxValue;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        string playerDirectory = Path.GetDirectoryName(report.summary.outputPath);
        string playerName = Path.GetFileNameWithoutExtension(report.summary.outputPath);
        if (string.IsNullOrEmpty(playerDirectory) || string.IsNullOrEmpty(playerName))
        {
            throw new BuildFailedException("CodePages DLL destination could not be determined.");
        }

        string destinationPath = Path.Combine(
            playerDirectory,
            playerName + "_Data",
            "Managed",
            AssemblyFileName);

        // IL2CPP builds do not have this Managed assembly destination.
        if (!File.Exists(destinationPath))
        {
            return;
        }

        string packagesDirectory = Path.Combine(Application.dataPath, "Packages");
        string sourcePath = Directory
            .EnumerateFiles(packagesDirectory, AssemblyFileName, SearchOption.AllDirectories)
            .Where(path => new FileInfo(path).Length >= MinimumImplementationSize)
            .OrderByDescending(path => new FileInfo(path).Length)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(sourcePath))
        {
            throw new BuildFailedException(
                $"The full {AssemblyFileName} implementation was not found under {packagesDirectory}.");
        }

        File.Copy(sourcePath, destinationPath, true);

        long sourceSize = new FileInfo(sourcePath).Length;
        long destinationSize = new FileInfo(destinationPath).Length;
        if (destinationSize != sourceSize)
        {
            throw new BuildFailedException(
                $"Failed to verify {AssemblyFileName} after copying it to the player build.");
        }

        Debug.Log(
            $"[CodePagesBuildPostprocessor] Installed {AssemblyFileName} " +
            $"({destinationSize:N0} bytes) into the Windows player build.");
    }
}
