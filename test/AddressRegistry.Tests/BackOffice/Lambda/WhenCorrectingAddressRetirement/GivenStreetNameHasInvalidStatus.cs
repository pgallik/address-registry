namespace AddressRegistry.Tests.BackOffice.Lambda.WhenCorrectingAddressRetirement
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AddressRegistry.Api.BackOffice.Abstractions.Requests;
    using AddressRegistry.Api.BackOffice.Handlers.Lambda.Handlers;
    using AddressRegistry.Api.BackOffice.Handlers.Lambda.Requests;
    using AddressRegistry.Api.BackOffice.Handlers.Sqs.Requests;
    using Autofac;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.GrAr.Provenance;
    using global::AutoFixture;
    using Infrastructure;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using StreetName;
    using StreetName.Exceptions;
    using TicketingService.Abstractions;
    using Xunit;
    using Xunit.Abstractions;

    public class GivenStreetNameHasInvalidStatus : BackOfficeLambdaTest
    {
        public GivenStreetNameHasInvalidStatus(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            Fixture.Customize(new WithFixedMunicipalityId());
        }

        [Fact]
        public async Task WhenStreetNameIsRemoved_ThenTicketingErrorIsExpected()
        {
            // Arrange
            var ticketing = new Mock<ITicketing>();

            var sut = new CorrectAddressRetirementLambdaHandler(
                Container.Resolve<IConfiguration>(),
                new FakeRetryPolicy(),
                ticketing.Object,
                Mock.Of<IStreetNames>(),
                MockExceptionIdempotentCommandHandler<StreetNameHasInvalidStatusException>().Object);

            // Act
            var request = new CorrectAddressRetirementLambdaRequest(Fixture.Create<int>().ToString(), new CorrectAddressRetirementSqsRequest()
            {
                Request = new CorrectAddressRetirementBackOfficeRequest(),
                TicketId = Guid.NewGuid(),
                Metadata = new Dictionary<string, object?>(),
                ProvenanceData = Fixture.Create<ProvenanceData>()
            });
            await sut.Handle(request, CancellationToken.None);

            //Assert
            ticketing.Verify(x =>
                x.Error(
                    It.IsAny<Guid>(),
                    new TicketError(
                        "Deze actie is enkel toegestaan binnen straatnamen met status 'voorgesteld' of 'inGebruik'.",
                        "AdresStraatnaamVoorgesteldOfInGebruik"),
                    CancellationToken.None));
        }
    }
}
