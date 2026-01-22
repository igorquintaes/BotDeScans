using BotDeScans.App.Features.GoogleDrive.InternalServices;

namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.InternalServices;

public class GoogleDriveQueryBuilderTests : UnitTest, IDisposable
{
    public GoogleDriveQueryBuilderTests()
    {
        GoogleDriveSettingsService.BaseFolderId = fixture.Create<string>();
    }

    public void Dispose()
    {
        GoogleDriveSettingsService.BaseFolderId = null!;
        GC.SuppressFinalize(this);
    }

    public class Build : GoogleDriveQueryBuilderTests
    {
        [Fact]
        public void GivenNoParametersShouldReturnQueryWithOnlyTrashedAndParentConditions()
        {
            var expectedQuery = $"trashed = false and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder().Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenMimeTypeShouldIncludeMimeTypeCondition()
        {
            var mimeType = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType = '{mimeType}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(mimeType)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenForbiddenMimeTypeShouldIncludeForbiddenMimeTypeCondition()
        {
            var forbiddenMimeType = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType != '{forbiddenMimeType}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithoutMimeType(forbiddenMimeType)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenNameShouldIncludeNameCondition()
        {
            var name = fixture.Create<string>();
            var expectedQuery = $"trashed = false and name = '{name}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithName(name)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenParentIdShouldUseCustomParentCondition()
        {
            var parentId = fixture.Create<string>();
            var expectedQuery = $"trashed = false and '{parentId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithParent(parentId)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenAllParametersShouldIncludeAllConditions()
        {
            var mimeType = fixture.Create<string>();
            var forbiddenMimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType = '{mimeType}' and mimeType != '{forbiddenMimeType}' and name = '{name}' and '{parentId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(mimeType)
                .WithoutMimeType(forbiddenMimeType)
                .WithName(name)
                .WithParent(parentId)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenNullMimeTypeShouldNotIncludeMimeTypeCondition()
        {
            var expectedQuery = $"trashed = false and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(null)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenNullForbiddenMimeTypeShouldNotIncludeForbiddenMimeTypeCondition()
        {
            var expectedQuery = $"trashed = false and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithoutMimeType(null)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenNullNameShouldNotIncludeNameCondition()
        {
            var expectedQuery = $"trashed = false and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithName(null)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenNullParentIdShouldUseBaseFolderIdAsDefault()
        {
            var expectedQuery = $"trashed = false and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithParent(null)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenMultipleCallsShouldReturnSameBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();

            var result1 = builder.WithMimeType("type1");
            var result2 = result1.WithName("name1");
            var result3 = result2.WithParent("parent1");

            result1.Should().BeSameAs(builder);
            result2.Should().BeSameAs(builder);
            result3.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenFluentCallsShouldBuildCorrectQuery()
        {
            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType = '{mimeType}' and name = '{name}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(mimeType)
                .WithName(name)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenMixedOrderOfCallsShouldMaintainCorrectConditionOrder()
        {
            var mimeType = fixture.Create<string>();
            var name = fixture.Create<string>();
            var parentId = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType = '{mimeType}' and name = '{name}' and '{parentId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithParent(parentId)
                .WithName(name)
                .WithMimeType(mimeType)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenOnlyMimeTypeAndForbiddenMimeTypeShouldBuildCorrectQuery()
        {
            var mimeType = fixture.Create<string>();
            var forbiddenMimeType = fixture.Create<string>();
            var expectedQuery = $"trashed = false and mimeType = '{mimeType}' and mimeType != '{forbiddenMimeType}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(mimeType)
                .WithoutMimeType(forbiddenMimeType)
                .Build();

            result.Should().Be(expectedQuery);
        }

        [Fact]
        public void GivenMultipleBuildCallsShouldReturnSameQuery()
        {
            var mimeType = fixture.Create<string>();
            var builder = new GoogleDriveQueryBuilder().WithMimeType(mimeType);

            var result1 = builder.Build();
            var result2 = builder.Build();

            result1.Should().Be(result2);
        }
    }

    public class WithMimeType : GoogleDriveQueryBuilderTests
    {
        [Fact]
        public void GivenValidMimeTypeShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();
            var mimeType = fixture.Create<string>();

            var result = builder.WithMimeType(mimeType);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenNullMimeTypeShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();

            var result = builder.WithMimeType(null);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenMultipleCallsShouldUseLastValue()
        {
            var mimeType1 = "type1";
            var mimeType2 = "type2";
            var expectedQuery = $"trashed = false and mimeType = '{mimeType2}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithMimeType(mimeType1)
                .WithMimeType(mimeType2)
                .Build();

            result.Should().Be(expectedQuery);
        }
    }

    public class WithoutMimeType : GoogleDriveQueryBuilderTests
    {
        [Fact]
        public void GivenValidForbiddenMimeTypeShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();
            var forbiddenMimeType = fixture.Create<string>();

            var result = builder.WithoutMimeType(forbiddenMimeType);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenNullForbiddenMimeTypeShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();

            var result = builder.WithoutMimeType(null);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenMultipleCallsShouldUseLastValue()
        {
            var forbiddenType1 = "type1";
            var forbiddenType2 = "type2";
            var expectedQuery = $"trashed = false and mimeType != '{forbiddenType2}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithoutMimeType(forbiddenType1)
                .WithoutMimeType(forbiddenType2)
                .Build();

            result.Should().Be(expectedQuery);
        }
    }

    public class WithName : GoogleDriveQueryBuilderTests
    {
        [Fact]
        public void GivenValidNameShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();
            var name = fixture.Create<string>();

            var result = builder.WithName(name);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenNullNameShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();

            var result = builder.WithName(null);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenMultipleCallsShouldUseLastValue()
        {
            var name1 = "name1";
            var name2 = "name2";
            var expectedQuery = $"trashed = false and name = '{name2}' and '{GoogleDriveSettingsService.BaseFolderId}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithName(name1)
                .WithName(name2)
                .Build();

            result.Should().Be(expectedQuery);
        }
    }

    public class WithParent : GoogleDriveQueryBuilderTests
    {
        [Fact]
        public void GivenValidParentIdShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();
            var parentId = fixture.Create<string>();

            var result = builder.WithParent(parentId);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenNullParentIdShouldReturnBuilderInstance()
        {
            var builder = new GoogleDriveQueryBuilder();

            var result = builder.WithParent(null);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void GivenMultipleCallsShouldUseLastValue()
        {
            var parent1 = "parent1";
            var parent2 = "parent2";
            var expectedQuery = $"trashed = false and '{parent2}' in parents";

            var result = new GoogleDriveQueryBuilder()
                .WithParent(parent1)
                .WithParent(parent2)
                .Build();

            result.Should().Be(expectedQuery);
        }
    }
}