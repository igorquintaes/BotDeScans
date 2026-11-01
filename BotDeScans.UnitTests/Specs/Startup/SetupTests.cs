//using BotDeScans.App.Services.Initializations;
//using BotDeScans.App.Services.Initializations.Setups;
//using FluentResults;

//namespace BotDeScans.UnitTests.Specs.Startup;

//public class SetupApplicationTests : UnitTest
//{
//    public class StartAsyncTests : SetupApplicationTests
//    {
//        private readonly SetupApplication setupApplication;
//        private readonly Result testResultsOutput;

//        public StartAsyncTests()
//        {
//            testResultsOutput = new Result();

//            Result.Setup(cfg =>
//            {
//                cfg.SuccessFactory = successMessage =>
//                {
//                    var success = new Success(successMessage);
//                    testResultsOutput.WithSuccess(success);

//                    return success;
//                };
//                cfg.ErrorFactory = errorMessage =>
//                {
//                    var error = new Error(errorMessage);
//                    testResultsOutput.WithError(error);

//                    return error;
//                };
//                cfg.ExceptionalErrorFactory = (message, ex) =>
//                {
//                    var error = new ExceptionalError(message, ex);
//                    testResultsOutput.WithError(error);

//                    return error;
//                };
//            });

//            var setup = fixture.FreezeFake<ISetup>();
//            IEnumerable<ISetup> setupCollection = new List<ISetup>([setup]);

//            fixture.Inject(setupCollection);

//            A.CallTo(() => fixture
//                .FreezeFake<ISetup>()
//                .ExecuteAsync(cancellationToken))
//                .Returns(Result.Ok());

//            A.CallTo(() => fixture
//                .FreezeFake<IServiceProvider>()
//                .GetService(typeof(GoogleDriveInitializer)))
//                .Returns(fixture.FreezeFake<GoogleDriveInitializer>());

//            A.CallTo(() => fixture
//                .FreezeFake<GoogleDriveInitializer>()
//                .ExecuteAsync(cancellationToken))
//                .Returns(Result.Ok());
//        }

//        [Fact]
//        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
//        {
//            await setupApplication.StartAsync(cancellationToken);

//            testResultsOutput.IsSuccess.Should().BeTrue();
//        }

//        [Fact]
//        public async Task GivenSuccessfulExecutionWithResultSuccessReturnSuccessResult()
//        {
//            const string SUCCESS_MESSAGE = "success message";

//            A.CallTo(() => fixture
//                .FreezeFake<ISetup>()
//                .ExecuteAsync(cancellationToken))
//                .Returns(Result.Ok().WithSuccess(SUCCESS_MESSAGE));

//            await setupApplication.StartAsync(cancellationToken);

//            testResultsOutput.Should().BeSuccess().And.HaveReason(SUCCESS_MESSAGE);
//        }

//        [Fact]
//        public async Task GivenSuccessfulExecutionWithClientsSetupShouldCallGoogleDriverInitializer()
//        {

//            var setup = fixture.FreezeFake<ClientsSetup>();
//            IEnumerable<ISetup> setupCollection = new List<ISetup>([setup]);

//            fixture.Inject(setupCollection);

//            A.CallTo(() => fixture
//                .FreezeFake<ClientsSetup>()
//                .ExecuteAsync(cancellationToken))
//                .Returns(Result.Ok());

//            await setupApplication.StartAsync(cancellationToken);

//            testResultsOutput.Should().BeSuccess();

//            A.CallTo(() => fixture
//                .FreezeFake<ClientsSetup>()
//                .ExecuteAsync(cancellationToken))
//                .MustHaveHappenedOnceExactly();
//        }

//        // todo
//    }
//}
