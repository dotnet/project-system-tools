using System;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts
{
    class RpcDescriptors
    {
        /// <summary>
        /// Gets the <see cref="ServiceRpcDescriptor"/> for the calculator service.
        /// Use the <see cref="ICalculator"/> interface for the client proxy for this service.
        /// </summary>
        public static ServiceRpcDescriptor LoggerServiceDescriptor { get; } = new ServiceJsonRpcDescriptor(
            new ServiceMoniker("LoggerService", new Version(1, 0)),
            ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader);
    }
}
