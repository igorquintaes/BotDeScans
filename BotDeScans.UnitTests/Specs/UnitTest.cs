using Bogus;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using System;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace BotDeScans.UnitTests.Specs
{
    public abstract class UnitTest<T> : UnitTest
    {
        #pragma warning disable CS8618
        protected T instance;
        #pragma warning restore CS8618
    }

    public abstract class UnitTest : IDisposable
    {
        protected static readonly Faker dataGenerator = new();
        protected CancellationToken cancellationToken = new();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    GoogleDriveSettingsService.BaseFolderId = null!;
                }

                disposedValue = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
