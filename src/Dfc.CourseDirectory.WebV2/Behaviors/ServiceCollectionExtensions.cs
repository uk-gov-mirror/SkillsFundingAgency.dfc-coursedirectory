﻿using System;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dfc.CourseDirectory.WebV2.Behaviors
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBehaviors(
            this IServiceCollection services,
            Assembly handlersAssembly)
        {
            foreach (var type in handlersAssembly.GetTypes())
            {
                RegisterRestrictQAStatusBehavior(type);
            }

            return services;

            void RegisterRestrictQAStatusBehavior(Type type)
            { 
                // For any type implementing IRestrictQAStatus<TRequest, TResponse>
                // add a RestrictQAStatusBehavior behavior for its request & response types

                var restrictQaStatusType = typeof(IRestrictQAStatus<>);

                var restrictTypes = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == restrictQaStatusType)
                    .ToList();

                foreach (var t in restrictTypes)
                {
                    var requestType = t.GenericTypeArguments[0];
                    var responseType = GetResponseTypeFromRequestType(requestType);

                    var pipelineBehaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
                    var behaviourType = typeof(RestrictQAStatusBehavior<,>).MakeGenericType(requestType, responseType);
                    services.AddScoped(pipelineBehaviorType, behaviourType);

                    // Register the handler as an IRestrictQAStatus<TRequest, TResponse>
                    var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
                    services.AddScoped(t, sp => sp.GetRequiredService(handlerInterfaceType));
                }
            }

            Type GetResponseTypeFromRequestType(Type requestType) =>
                requestType
                    .GetInterfaces()
                    .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    .GetGenericArguments()[0];
        }
    }
}
