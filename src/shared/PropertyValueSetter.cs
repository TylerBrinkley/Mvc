﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    internal static class PropertyValueSetter
    {
        private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
            typeof(PropertyValueSetter).GetMethod(nameof(CallPropertyAddRange));

        public static void SetValue(
            ModelMetadata metadata,
            PropertyHelper propertyHelper,
            object instance,
            object value)
        {
            var propertyType = propertyHelper.Property.PropertyType;

            if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPublic == true)
            {
                // Handle settable property. Do not set the property to null if the type is a non-nullable type.
                if (value != null || metadata.IsReferenceOrNullableType)
                {
                    propertyHelper.SetValue(instance, value);
                }

                return;
            }

            if (propertyType.IsArray)
            {
                // Do not attempt to copy values into an array because an array's length is immutable. This choice
                // is also consistent with MutableObjectModelBinder's handling of a read-only array property.
                return;
            }

            var target = propertyHelper.GetValue(instance);
            if (value == null || target == null)
            {
                // Nothing to do when source or target is null.
                return;
            }

            if (!metadata.IsCollectionType)
            {
                // Not a collection model.
                return;
            }

            // Handle a read-only collection property.
            var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
                metadata.ElementMetadata.ModelType);
            propertyAddRange.Invoke(obj: null, parameters: new[] { target, value });
        }

        // Called via reflection.
        private static void CallPropertyAddRange<TElement>(object target, object source)
        {
            var targetCollection = (ICollection<TElement>)target;
            var sourceCollection = source as IEnumerable<TElement>;
            if (sourceCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var item in sourceCollection)
                {
                    targetCollection.Add(item);
                }
            }
        }
    }
}
