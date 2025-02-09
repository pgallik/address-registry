namespace AddressRegistry.StreetName.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class HouseNumberHasInvalidFormatException : AddressRegistryException
    {
        public HouseNumberHasInvalidFormatException()
        { }

        private HouseNumberHasInvalidFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public HouseNumberHasInvalidFormatException(string message)
            : base(message)
        { }

        public HouseNumberHasInvalidFormatException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}
