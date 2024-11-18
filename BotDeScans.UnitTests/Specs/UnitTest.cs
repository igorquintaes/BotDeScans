using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using Bogus;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using System;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace BotDeScans.UnitTests.Specs;

public abstract class UnitTest
{
    protected readonly IFixture fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
    protected readonly Faker dataGenerator = new();
    protected CancellationToken cancellationToken = new();
}
