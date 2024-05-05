﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Services;

using TestingPlatformExplorer.In_process_extensions;
using TestingPlatformExplorer.TestingFramework;

// Create the test application builder
ITestApplicationBuilder testApplicationBuilder = await TestApplication.CreateBuilderAsync(args);

// Register the testing framework
testApplicationBuilder.AddTestingFramework(new[] { Assembly.GetExecutingAssembly() });

// In-process & out-of-process extensions
// Register the testing framework command line options
testApplicationBuilder.CommandLine.AddProvider(() => new TestingFrameworkCommandLineOptions());

// In-process extensions
testApplicationBuilder.TestHost.AddTestApplicationLifecycleCallbacks(serviceProvider
    => new DisplayTestApplicationLifecycleCallbacks(serviceProvider.GetOutputDevice()));
testApplicationBuilder.TestHost.AddTestSessionLifetimeHandle(serviceProvider
    => new DisplayTestSessionLifeTimeHandler(serviceProvider.GetOutputDevice()));
testApplicationBuilder.TestHost.AddDataConsumer(serviceProvider
    => new DisplayDataConsumer(serviceProvider.GetOutputDevice()));

using ITestApplication testApplication = await testApplicationBuilder.BuildAsync();
return await testApplication.RunAsync();
