namespace AddressRegistry.Tests.BackOffice.Api.WhenProposingAddress
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Address;
    using AddressRegistry.Api.BackOffice.Abstractions.Requests;
    using FluentAssertions;
    using FluentValidation;
    using FluentValidation.Results;
    using Moq;
    using StreetName;
    using StreetName.Exceptions;
    using Xunit;
    using Xunit.Abstractions;
    using AddressController = AddressRegistry.Api.BackOffice.AddressController;

    public class GivenParentAddressAlreadyExists : BackOfficeApiTest
    {
        private readonly AddressController _controller;

        public GivenParentAddressAlreadyExists(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _controller = CreateApiBusControllerWithUser<AddressController>();
        }

        [Fact]
        public void ThenThrowsValidationException()
        {
            var streetNamePersistentId = new StreetNamePersistentLocalId(123);
            var postInfoId = new PersistentLocalId(456);

            var mockRequestValidator = new Mock<IValidator<AddressProposeRequest>>();
            mockRequestValidator.Setup(x => x.ValidateAsync(It.IsAny<AddressProposeRequest>(), CancellationToken.None))
                .Returns(Task.FromResult(new ValidationResult()));

            MockMediator.Setup(x => x.Send(It.IsAny<AddressProposeRequest>(), CancellationToken.None))
                .Throws(new ParentAddressAlreadyExistsException(new AddressRegistry.StreetName.HouseNumber("11")));

            var body = new AddressProposeRequest
            {
                StraatNaamId = $"https://data.vlaanderen.be/id/straatnaam/{streetNamePersistentId}",
                PostInfoId = $"https://data.vlaanderen.be/id/postinfo/{postInfoId}",
                Huisnummer = "11"
            };

            //Act
            Func<Task> act = async () => await _controller.Propose(
                ResponseOptions,
                mockRequestValidator.Object,
                body);

            // Assert
            act
                .Should()
                .ThrowAsync<ValidationException>()
                .Result
                .Where(x =>
                    x.Errors.Any(
                        failure => failure.ErrorCode == "AdresBestaandeHuisnummerBusnummerCombinatie"
                                   && failure.ErrorMessage == "Deze combinatie huisnummer-busnummer bestaat reeds voor de opgegeven straatnaam."
                                   && failure.PropertyName == nameof(body.Huisnummer)));
        }
    }
}
