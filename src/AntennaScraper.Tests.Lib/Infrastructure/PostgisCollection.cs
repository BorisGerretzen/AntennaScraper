namespace AntennaScraper.Tests.Lib.Infrastructure;

[CollectionDefinition("PostGIS", DisableParallelization = true)]
public sealed class PostgisCollection : ICollectionFixture<PostgisFixture>
{
}