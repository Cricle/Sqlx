// -----------------------------------------------------------------------
// <copyright file="SqlxPackage.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Sqlx.VisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined in the Visual Studio SDK to do this.
    /// It derives from the Package class that provides the implementation of the IVsPackage
    /// interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(SqlxPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class SqlxPackage : AsyncPackage
    {
        /// <summary>
        /// SqlxPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a8b1c2d3-e4f5-6789-a0b1-c2d3e4f56789";

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on the VS shell services.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize Sqlx IntelliSense services
            await InitializeSqlxServicesAsync();
        }

        /// <summary>
        /// Initializes Sqlx-specific services for IntelliSense support.
        /// </summary>
        private async Task InitializeSqlxServicesAsync()
        {
            await Task.Run(() =>
            {
                // Initialize the Sqlx IntelliSense services
                var sqlxService = new SqlxIntelliSenseService();
                sqlxService.Initialize();

                // Register the completion source providers
                // This will be handled by MEF composition in the language service components
            });
        }
    }

    /// <summary>
    /// Service that provides IntelliSense support for Sqlx.
    /// </summary>
    internal class SqlxIntelliSenseService
    {
        /// <summary>
        /// Initializes the Sqlx IntelliSense service.
        /// </summary>
        public void Initialize()
        {
            // Initialize database metadata cache
            // Initialize SQL parser and analyzer
            // Set up completion providers
        }
    }
}

