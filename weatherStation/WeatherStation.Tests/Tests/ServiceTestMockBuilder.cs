using System;
using System.Linq.Expressions;
using Moq;

namespace WeatherStation.Tests.Tests;

/// <summary>
/// Helper for creating Moq mocks.
/// </summary>
/// <typeparam name="TInterface">Interface type to mock.</typeparam>
public class ServiceTestMockBuilder<TInterface> where TInterface : class
{
    /// <summary>
    /// Builder for configuring mock setup.
    /// </summary>
    public class Builder
    {
        private readonly Mock<TInterface> mock = new Mock<TInterface>();

        /// <summary>
        /// Sets up a void method.
        /// </summary>
        /// <param name="expression">Method expression.</param>
        /// <returns>Builder instance.</returns>
        public Builder SetupVoid(Expression<Action<TInterface>> expression)
        {
            this.mock.Setup(expression);
            return this;
        }

        /// <summary>
        /// Sets up a method with return value.
        /// </summary>
        /// <typeparam name="TResult">Return type.</typeparam>
        /// <param name="expression">Method expression.</param>
        /// <param name="result">Return value.</param>
        /// <returns>Builder instance.</returns>
        public Builder Setup<TResult>(
            Expression<Func<TInterface, TResult>> expression,
            TResult result)
        {
            this.mock.Setup(expression).Returns(result);
            return this;
        }

        /// <summary>
        /// Sets up a method to throw an exception.
        /// </summary>
        /// <typeparam name="TResult">Return type.</typeparam>
        /// <param name="expression">Method expression.</param>
        /// <param name="ex">Exception to throw.</param>
        /// <returns>Builder instance.</returns>
        public Builder SetupException<TResult>(
            Expression<Func<TInterface, TResult>> expression,
            Exception ex)
        {
            this.mock.Setup(expression).Throws(ex);
            return this;
        }

        /// <summary>
        /// Builds the mock.
        /// </summary>
        /// <returns>Configured mock.</returns>
        public Mock<TInterface> Build()
        {
            return this.mock;
        }
    }
}
