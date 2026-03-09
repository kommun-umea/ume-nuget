#pragma warning disable CS0612 // Type or member is obsolete
using Shouldly;
using Umea.se.TestToolkit.Mocks;
using Umea.se.Toolkit.Cache;
using Umea.se.Toolkit.ClockInterface;
using Umea.se.Toolkit.CommonModels;

namespace Umea.se.Toolkit.Test;

public class CacheTests
{
    private readonly RealDataProvider _realDataProvider = new();

    private readonly Personnummer _ssNo1 = "123456789111";
    private readonly Personnummer _ssNo2 = "121212129111";
    private readonly Personnummer _ssNo3 = "987654321111";

    private Func<Personnummer, Task<PersonsData>> GenFunc => _realDataProvider.GenerateTestPerson;

    private static Cache<Personnummer, PersonsData> CreateCacheUnderTest(IClock? clockMock = null, int size = 5)
        => new(clockMock ?? new ClockMock(), size, TimeSpan.FromHours(24));

    private class PersonsData
    {
        public PersonsData(Personnummer ssn)
        {
            Ssn = ssn;
        }

        internal Personnummer Ssn { get; }
    }

    private class RealDataProvider
    {
        public int CallCount { get; private set; }

        public Task<PersonsData> GenerateTestPerson(Personnummer ssn)
        {
            CallCount++;

            PersonsData person = new(ssn);

            return Task.FromResult(person);
        }
    }

    [Fact]
    public async Task InitialCacheAccess_CallsDataProvider()
    {
        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest();

        PersonsData personDetails = await sut.GetData(_ssNo1, GenFunc);

        personDetails.ShouldNotBeNull();
        personDetails.Ssn.ShouldBe(_ssNo1);
        _realDataProvider.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task MultipleCacheAccess_CallsDataProvider()
    {
        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest();

        PersonsData personDetails1 = await sut.GetData(_ssNo1, GenFunc);
        PersonsData personDetails2 = await sut.GetData(_ssNo2, GenFunc);
        PersonsData personDetails3 = await sut.GetData(_ssNo3, GenFunc);

        personDetails1.ShouldNotBeNull();
        personDetails1.Ssn.ShouldBe(_ssNo1);
        personDetails2.ShouldNotBeNull();
        personDetails2.Ssn.ShouldBe(_ssNo2);
        personDetails3.ShouldNotBeNull();
        personDetails3.Ssn.ShouldBe(_ssNo3);
        _realDataProvider.CallCount.ShouldBe(3);
    }

    [Fact]
    public async Task CacheInvalidation_EmptiesCache()
    {
        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest();

        PersonsData personDetails1 = await sut.GetData(_ssNo1, GenFunc);
        sut.InvalidateCache();
        PersonsData personDetails2 = await sut.GetData(_ssNo1, GenFunc);

        personDetails1.ShouldNotBeNull();
        personDetails1.Ssn.ShouldBe(_ssNo1);
        personDetails2.ShouldNotBeNull();
        personDetails2.Ssn.ShouldBe(_ssNo1);
        _realDataProvider.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task RepeatedCacheAccess_DoesNotCallProviderRepeatedly()
    {
        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest();

        PersonsData personDetails1 = await sut.GetData(_ssNo1, GenFunc);
        PersonsData personDetails2 = await sut.GetData(_ssNo1, GenFunc);
        PersonsData personDetails3 = await sut.GetData(_ssNo1, GenFunc);

        personDetails1.ShouldNotBeNull();
        personDetails1.Ssn.ShouldBe(_ssNo1);
        personDetails2.ShouldNotBeNull();
        personDetails2.Ssn.ShouldBe(_ssNo1);
        personDetails3.ShouldNotBeNull();
        personDetails3.Ssn.ShouldBe(_ssNo1);
        _realDataProvider.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CacheStoresOnlyUpToSize()
    {
        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest(size: 2);

        PersonsData personDetails1 = await sut.GetData(_ssNo1, GenFunc);
        PersonsData personDetails2 = await sut.GetData(_ssNo2, GenFunc);
        PersonsData personDetails3 = await sut.GetData(_ssNo3, GenFunc);
        PersonsData personDetails4 = await sut.GetData(_ssNo1, GenFunc);

        personDetails1.ShouldNotBeNull();
        personDetails1.Ssn.ShouldBe(_ssNo1);
        personDetails2.ShouldNotBeNull();
        personDetails2.Ssn.ShouldBe(_ssNo2);
        personDetails3.ShouldNotBeNull();
        personDetails3.Ssn.ShouldBe(_ssNo3);
        personDetails4.ShouldNotBeNull();
        personDetails4.Ssn.ShouldBe(_ssNo1);
        _realDataProvider.CallCount.ShouldBe(4);
    }

    [Fact]
    public async Task CacheInvalidatesAfterOneDay()
    {
        ClockMock clockMock = new();
        DateTime noon = DateTime.Today.AddHours(12);

        Cache<Personnummer, PersonsData> sut = CreateCacheUnderTest(clockMock, size: 2);

        clockMock.MockedNow = noon;
        await sut.GetData(_ssNo1, GenFunc);
        _realDataProvider.CallCount.ShouldBe(1);

        clockMock.MockedNow = noon.AddHours(23);
        await sut.GetData(_ssNo1, GenFunc);
        _realDataProvider.CallCount.ShouldBe(1);

        clockMock.MockedNow = noon.AddHours(25);
        await sut.GetData(_ssNo1, GenFunc);
        _realDataProvider.CallCount.ShouldBe(2);
    }
}
#pragma warning restore CS0612 // Type or member is obsolete
