﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BGC.Extensions.Linq
{
    public static class EnumerableExtensions
    {
        public static float GeometricMean(this IEnumerable<float> source) => 
            (float)Math.Pow(source.Aggregate(1f, ProductAccumulate), 1.0 / source.Count());
        public static double GeometricMean(this IEnumerable<double> source) =>
            Math.Pow(source.Aggregate(1.0, ProductAccumulate), 1.0 / source.Count());
        public static IEnumerable<T> ToSingleItemEnumberable<T>(this T item)
        {
            yield return item;
        }

        public static float ProductAccumulate(float total, float value) => total * value;
        public static double ProductAccumulate(double total, double value) => total * value;
    }
}
