using TimeInChinaPlugin;
using System.Reflection;

// AssemblyVersion = full version info, major.minor.patch
[assembly: AssemblyVersion(GlobalAssemblyInfo.SimpleVersion)]

// FileVersion = full version info, major.minor.patch
[assembly: AssemblyFileVersion(GlobalAssemblyInfo.SimpleVersion)]

// InformationalVersion = full version + branch + commit sha.
[assembly: AssemblyInformationalVersion(GlobalAssemblyInfo.InformationalVersion)]

namespace TimeInChinaPlugin
{
    /// <summary>
    /// <see cref="GlobalAssemblyInfo"/> documents the build version of the plugin
    /// </summary>
    public class GlobalAssemblyInfo
    {
        /// <summary>
        /// Simple release-like version number, like 4.0.1.0.
        /// </summary>
        public const string SimpleVersion = ThisAssembly.Git.BaseVersion.Major + "." + ThisAssembly.Git.BaseVersion.Minor + "." + ThisAssembly.Git.BaseVersion.Patch + "." + ThisAssembly.Git.Commits;

        /// <summary>
        /// Full version, plus branch and commit short sha, like 4.0.1.0-39cf84e-branch.
        /// </summary>
        public const string InformationalVersion = SimpleVersion + "-" + ThisAssembly.Git.Commit + "+" + ThisAssembly.Git.Branch;
    }
}